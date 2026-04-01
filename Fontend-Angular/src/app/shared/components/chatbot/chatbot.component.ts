import {
  Component, inject, signal, computed, ViewChild,
  ElementRef, AfterViewChecked, OnInit, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, NavigationStart } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { ChatbotService, ChatMessage } from '../../../core/services/chatbot.service';
import {
  GUEST_CONTEXT, ADMIN_CONTEXT, SUPERADMIN_CONTEXT, PUBLIC_CONTEXT
} from '../../../core/services/chatbot-prompts';

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.scss']
})
export class ChatbotComponent implements OnInit, AfterViewChecked, OnDestroy {
  private auth = inject(AuthService);
  private chatbotService = inject(ChatbotService);
  private router = inject(Router);

  @ViewChild('messagesContainer') private messagesContainer!: ElementRef<HTMLDivElement>;

  isOpen = signal(false);
  messages = signal<ChatMessage[]>([]);
  userInput = signal('');
  loading = signal(false);
  private shouldScroll = false;
  private routerSub!: Subscription;

  readonly userName = computed(() => this.auth.currentUser()?.userName ?? null);
  readonly role = computed(() => this.auth.currentUser()?.role ?? null);

  private get systemPrompt(): string {
    const role = this.role();
    if (role === 'Guest') return GUEST_CONTEXT;
    if (role === 'Admin') return ADMIN_CONTEXT;
    if (role === 'SuperAdmin') return SUPERADMIN_CONTEXT;
    return PUBLIC_CONTEXT;
  }

  private get greeting(): string {
    const name = this.userName();
    const role = this.role();
    if (role === 'Admin') return `Hello ${name}, Hotel Admin! 👋 How can I help you today?`;
    if (role === 'SuperAdmin') return `Hello ${name}, SuperAdmin! 👋 How can I help you today?`;
    if (role === 'Guest') return `Hi ${name}! 👋 How can I help you today?`;
    return `Hi there! 👋 I'm Thanush StayHub AI. How can I help you?`;
  }

  ngOnInit(): void {
    this.messages.set([{ role: 'model', text: this.greeting }]);

    // Auto-close chatbot on any route navigation
    this.routerSub = this.router.events.pipe(
      filter(event => event instanceof NavigationStart)
    ).subscribe(() => {
      if (this.isOpen()) this.isOpen.set(false);
    });
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  toggle(): void {
    this.isOpen.update(v => !v);
    if (this.isOpen()) {
      this.shouldScroll = true;
    }
  }

  setInput(value: string): void {
    this.userInput.set(value);
  }

  send(): void {
    const text = this.userInput().trim();
    if (!text || this.loading()) return;

    const currentMessages = this.messages();
    const newMessages: ChatMessage[] = [...currentMessages, { role: 'user', text }];
    this.messages.set(newMessages);
    this.userInput.set('');
    this.loading.set(true);
    this.shouldScroll = true;

    const history = newMessages.slice(1, -1);

    this.chatbotService.send(history, text, this.systemPrompt).subscribe({
      next: (reply) => {
        this.messages.update(msgs => [...msgs, { role: 'model', text: reply }]);
        this.loading.set(false);
        this.shouldScroll = true;
      },
      error: () => {
        this.messages.update(msgs => [
          ...msgs,
          { role: 'model', text: 'I apologize, something went wrong on our end. Please try again in a moment.' }
        ]);
        this.loading.set(false);
        this.shouldScroll = true;
      }
    });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  clearChat(): void {
    this.messages.set([{ role: 'model', text: this.greeting }]);
  }

  private scrollToBottom(): void {
    try {
      const el = this.messagesContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }

  formatText(text: string): string {
    return text
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n/g, '<br>');
  }
}
