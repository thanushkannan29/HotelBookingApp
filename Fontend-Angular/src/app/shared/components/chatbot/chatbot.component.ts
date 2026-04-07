import {
  Component, inject, signal, computed, ViewChild,
  ElementRef, AfterViewChecked, OnInit, OnDestroy, ChangeDetectorRef
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, NavigationStart } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { ChatbotService, ChatMessage } from '../../../core/services/chatbot.service';
import {
  GUEST_CONTEXT, ADMIN_CONTEXT, SUPERADMIN_CONTEXT, PUBLIC_CONTEXT
} from '../../../core/services/chatbot-prompts';

export interface ChatMessageWithMeta extends ChatMessage {
  timestamp: Date;
  /** Displayed text — grows during typewriter animation */
  displayText: string;
  /** Whether the typewriter is still running */
  typing?: boolean;
  /** Whether this message errored and can be retried */
  isError?: boolean;
  /** Follow-up suggestion chips shown below this bot message */
  followUps?: string[];
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.scss']
})
export class ChatbotComponent implements OnInit, AfterViewChecked, OnDestroy {
  private auth    = inject(AuthService);
  private chatbot = inject(ChatbotService);
  private router  = inject(Router);
  private cdr     = inject(ChangeDetectorRef);

  @ViewChild('messagesContainer') private messagesContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('inputRef')          private inputRef!: ElementRef<HTMLTextAreaElement>;

  isOpen      = signal(false);
  messages    = signal<ChatMessageWithMeta[]>([]);
  userInput   = signal('');
  loading     = signal(false);
  unreadCount = signal(0);
  copiedIndex = signal<number | null>(null);

  private shouldScroll = false;
  private routerSub!: Subscription;
  /** The raw text of the last user message — used for retry */
  private lastUserText = '';

  readonly userName = computed(() => this.auth.currentUser()?.userName ?? null);
  readonly role     = computed(() => this.auth.currentUser()?.role ?? null);

  private get systemPrompt(): string {
    const r = this.role();
    if (r === 'Guest')      return GUEST_CONTEXT;
    if (r === 'Admin')      return ADMIN_CONTEXT;
    if (r === 'SuperAdmin') return SUPERADMIN_CONTEXT;
    return PUBLIC_CONTEXT;
  }

  private get greeting(): string {
    const name = this.userName();
    const role = this.role();
    if (role === 'Admin')      return `Hello ${name}! 👋 I'm your StayHub assistant. How can I help you manage your hotel today?`;
    if (role === 'SuperAdmin') return `Hello ${name}! 👋 I'm your StayHub assistant. How can I help you with platform management?`;
    if (role === 'Guest')      return `Hi ${name}! 👋 I'm your StayHub assistant. How can I help with your booking today?`;
    return `Hi there! 👋 I'm the Thanush StayHub AI assistant. How can I help you today?`;
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.messages.set([this.makeMsg('model', this.greeting)]);

    this.routerSub = this.router.events.pipe(
      filter(e => e instanceof NavigationStart)
    ).subscribe(() => { if (this.isOpen()) this.isOpen.set(false); });
  }

  ngOnDestroy(): void { this.routerSub?.unsubscribe(); }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) { this.scrollToBottom(); this.shouldScroll = false; }
  }

  // ── Public actions ─────────────────────────────────────────────────────────

  toggle(): void {
    this.isOpen.update(v => !v);
    if (this.isOpen()) {
      this.unreadCount.set(0);
      this.shouldScroll = true;
      setTimeout(() => this.inputRef?.nativeElement?.focus(), 120);
    }
  }

  setInput(value: string): void {
    this.userInput.set(value);
    this.autoResize();
  }

  onInput(event: Event): void {
    this.userInput.set((event.target as HTMLTextAreaElement).value);
    this.autoResize();
  }

  send(overrideText?: string): void {
    const text = (overrideText ?? this.userInput()).trim();
    if (!text || this.loading()) return;

    this.lastUserText = text;

    const current = this.messages();
    const updated: ChatMessageWithMeta[] = [
      ...current,
      this.makeMsg('user', text)
    ];
    this.messages.set(updated);
    this.userInput.set('');
    this.loading.set(true);
    this.shouldScroll = true;

    if (this.inputRef?.nativeElement) {
      this.inputRef.nativeElement.style.height = 'auto';
    }

    const history: ChatMessage[] = updated.slice(1, -1).map(m => ({
      role: m.role, text: m.text
    }));

    this.chatbot.send(history, text, this.systemPrompt).subscribe({
      next: reply => {
        const followUps = this.extractFollowUps(reply, text);
        const msg = this.makeMsg('model', reply, { followUps });
        this.messages.update(msgs => [...msgs, msg]);
        this.loading.set(false);
        this.shouldScroll = true;
        if (!this.isOpen()) this.unreadCount.update(n => n + 1);
        this.animateTypewriter(msg);
      },
      error: () => {
        const errMsg = this.makeMsg('model',
          'something went wrong on our end. Please try again.',
          { isError: true }
        );
        this.messages.update(msgs => [...msgs, errMsg]);
        this.loading.set(false);
        this.shouldScroll = true;
      }
    });
  }

  retry(): void {
    // Remove the last error message and resend
    this.messages.update(msgs => msgs.filter(m => !m.isError));
    this.send(this.lastUserText);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  clearChat(): void {
    this.messages.set([this.makeMsg('model', this.greeting)]);
    this.unreadCount.set(0);
    this.lastUserText = '';
  }

  copyMessage(index: number, text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedIndex.set(index);
      setTimeout(() => this.copiedIndex.set(null), 2000);
    });
  }

  showTimestamp(index: number): boolean {
    if (index === 0) return true;
    const msgs = this.messages();
    return (msgs[index].timestamp.getTime() - msgs[index - 1].timestamp.getTime()) > 60_000;
  }

  formatText(text: string): string {
    return text
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n/g, '<br>');
  }

  // ── Private helpers ────────────────────────────────────────────────────────

  private makeMsg(
    role: 'user' | 'model',
    text: string,
    extras: Partial<ChatMessageWithMeta> = {}
  ): ChatMessageWithMeta {
    return { role, text, displayText: role === 'user' ? text : '', timestamp: new Date(), ...extras };
  }

  private animateTypewriter(msg: ChatMessageWithMeta): void {
    const full = msg.text;
    const charsPerTick = 3;
    let pos = 0;
    msg.typing = true;

    const tick = () => {
      pos = Math.min(pos + charsPerTick, full.length);
      msg.displayText = full.slice(0, pos);
      this.messages.update(msgs => [...msgs]); // trigger change detection
      this.shouldScroll = true;

      if (pos < full.length) {
        requestAnimationFrame(tick);
      } else {
        msg.typing = false;
        this.cdr.markForCheck();
      }
    };

    requestAnimationFrame(tick);
  }

  /**
   * Extracts 2 contextual follow-up suggestions based on keywords in the reply.
   * Falls back to role-based defaults.
   */
  private extractFollowUps(reply: string, question: string): string[] {
    const r = reply.toLowerCase();
    const q = question.toLowerCase();

    if (r.includes('cancel') || q.includes('cancel'))
      return ['What is the refund timeline?', 'How do I cancel with protection?'];
    if (r.includes('wallet') || q.includes('wallet'))
      return ['How do I top up my wallet?', 'Can I use wallet for full payment?'];
    if (r.includes('promo') || q.includes('promo'))
      return ['When do promo codes expire?', 'Can I use a promo on any hotel?'];
    if (r.includes('review') || q.includes('review'))
      return ['How do I write a review?', 'What reward do I get for reviewing?'];
    if (r.includes('payment') || r.includes('pay') || q.includes('pay'))
      return ['What payment methods are accepted?', 'What happens if payment fails?'];
    if (r.includes('reservation') || r.includes('booking') || q.includes('book'))
      return ['How do I view my booking?', 'What does Pending status mean?'];
    if (r.includes('inventory') || q.includes('inventory'))
      return ['How do I set room rates?', 'How do I add more rooms?'];
    if (r.includes('hotel') && this.role() === 'SuperAdmin')
      return ['How do I block a hotel?', 'How is commission calculated?'];

    // Role-based defaults
    const role = this.role();
    if (role === 'Guest')      return ['How do I cancel a booking?', 'How does the wallet work?'];
    if (role === 'Admin')      return ['How do I confirm a reservation?', 'How do I set room pricing?'];
    if (role === 'SuperAdmin') return ['How do I block a hotel?', 'How is revenue calculated?'];
    return ['How do I create an account?', 'What is the cancellation policy?'];
  }

  private autoResize(): void {
    const el = this.inputRef?.nativeElement;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 100) + 'px';
  }

  private scrollToBottom(): void {
    try {
      const el = this.messagesContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
