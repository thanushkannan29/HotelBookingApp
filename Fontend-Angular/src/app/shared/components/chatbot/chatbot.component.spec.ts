import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ChatbotComponent } from './chatbot.component';
import { ChatbotService } from '../../../core/services/chatbot.service';
import { AuthService } from '../../../core/services/auth.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

describe('ChatbotComponent', () => {
  let component: ChatbotComponent;
  let fixture: ComponentFixture<ChatbotComponent>;
  let chatbotSpy: jasmine.SpyObj<ChatbotService>;
  let authSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    chatbotSpy = jasmine.createSpyObj('ChatbotService', ['send']);
    authSpy    = jasmine.createSpyObj('AuthService', [], {
      currentUser: () => ({ userName: 'Alice', role: 'Guest' })
    });

    chatbotSpy.send.and.returnValue(of('Hello! How can I help?'));

    await TestBed.configureTestingModule({
      imports: [ChatbotComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: ChatbotService, useValue: chatbotSpy },
        { provide: AuthService,    useValue: authSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ChatbotComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── Initial state ─────────────────────────────────────────────────────────

  it('isOpen — should start as false', () => expect(component.isOpen()).toBeFalse());
  it('loading — should start as false', () => expect(component.loading()).toBeFalse());
  it('userInput — should start as empty string', () => expect(component.userInput()).toBe(''));

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should set greeting message', () => {
    expect(component.messages().length).toBe(1);
    expect(component.messages()[0].role).toBe('model');
  });

  it('ngOnInit — should include user name in greeting for Guest', () => {
    expect(component.messages()[0].text).toContain('Alice');
  });

  // ── toggle ────────────────────────────────────────────────────────────────

  it('toggle — should open chatbot', () => {
    component.toggle();
    expect(component.isOpen()).toBeTrue();
  });

  it('toggle — should close chatbot when already open', () => {
    component.toggle();
    component.toggle();
    expect(component.isOpen()).toBeFalse();
  });

  // ── setInput ──────────────────────────────────────────────────────────────

  it('setInput — should update userInput signal', () => {
    component.setInput('Hello');
    expect(component.userInput()).toBe('Hello');
  });

  // ── send ──────────────────────────────────────────────────────────────────

  it('send — should call chatbotService.send', () => {
    component.setInput('What are the amenities?');
    component.send();
    expect(chatbotSpy.send).toHaveBeenCalled();
  });

  it('send — should add user message to messages', () => {
    component.setInput('What are the amenities?');
    component.send();
    const userMsg = component.messages().find(m => m.role === 'user');
    expect(userMsg?.text).toBe('What are the amenities?');
  });

  it('send — should add model reply to messages', () => {
    component.setInput('What are the amenities?');
    component.send();
    const modelMsgs = component.messages().filter(m => m.role === 'model');
    expect(modelMsgs.length).toBeGreaterThan(1);
  });

  it('send — should clear userInput after sending', () => {
    component.setInput('Hello');
    component.send();
    expect(component.userInput()).toBe('');
  });

  it('send — should reset loading to false on success', () => {
    component.setInput('Hello');
    component.send();
    expect(component.loading()).toBeFalse();
  });

  it('send — should NOT send when input is empty', () => {
    component.setInput('');
    component.send();
    expect(chatbotSpy.send).not.toHaveBeenCalled();
  });

  it('send — should NOT send when loading is true', () => {
    component.loading.set(true);
    component.setInput('Hello');
    component.send();
    expect(chatbotSpy.send).not.toHaveBeenCalled();
  });

  it('send — should add error message on API failure', () => {
    chatbotSpy.send.and.returnValue(throwError(() => new Error('fail')));
    component.setInput('Hello');
    component.send();
    const lastMsg = component.messages().at(-1);
    expect(lastMsg?.role).toBe('model');
    expect(lastMsg?.text).toContain('something went wrong');
  });

  it('send — should reset loading to false on error', () => {
    chatbotSpy.send.and.returnValue(throwError(() => new Error('fail')));
    component.setInput('Hello');
    component.send();
    expect(component.loading()).toBeFalse();
  });

  // ── clearChat ─────────────────────────────────────────────────────────────

  it('clearChat — should reset messages to just the greeting', () => {
    component.setInput('Hello');
    component.send();
    component.clearChat();
    expect(component.messages().length).toBe(1);
    expect(component.messages()[0].role).toBe('model');
  });

  // ── formatText ────────────────────────────────────────────────────────────

  it('formatText — should convert **bold** to <strong>', () => {
    expect(component.formatText('**hello**')).toBe('<strong>hello</strong>');
  });

  it('formatText — should convert *italic* to <em>', () => {
    expect(component.formatText('*hello*')).toBe('<em>hello</em>');
  });

  it('formatText — should convert `code` to <code>', () => {
    expect(component.formatText('`hello`')).toBe('<code>hello</code>');
  });

  it('formatText — should convert newlines to <br>', () => {
    expect(component.formatText('line1\nline2')).toBe('line1<br>line2');
  });

  // ── onKeydown ─────────────────────────────────────────────────────────────

  it('onKeydown — should call send on Enter key', () => {
    spyOn(component, 'send');
    component.setInput('Hello');
    const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: false });
    spyOn(event, 'preventDefault');
    component.onKeydown(event);
    expect(component.send).toHaveBeenCalled();
  });

  it('onKeydown — should NOT call send on Shift+Enter', () => {
    spyOn(component, 'send');
    component.setInput('Hello');
    const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: true });
    component.onKeydown(event);
    expect(component.send).not.toHaveBeenCalled();
  });

  // ── computed ──────────────────────────────────────────────────────────────

  it('userName — should return user name from auth', () => {
    expect(component.userName()).toBe('Alice');
  });

  it('role — should return role from auth', () => {
    expect(component.role()).toBe('Guest');
  });
});
