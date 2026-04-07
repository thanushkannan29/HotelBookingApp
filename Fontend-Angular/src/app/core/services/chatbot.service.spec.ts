import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ChatbotService, ChatMessage } from './chatbot.service';
import { environment } from '../../../environments/environment';

const GROQ_URL = environment.groqApiUrl;

describe('ChatbotService', () => {
  let service: ChatbotService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ChatbotService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => expect(service).toBeTruthy());

  // ── send ──────────────────────────────────────────────────────────────────

  it('send — should POST to Groq API', () => {
    service.send([], 'Hello', 'You are a helpful assistant.').subscribe();
    const req = http.expectOne(GROQ_URL);
    expect(req.request.method).toBe('POST');
    req.flush({ choices: [{ message: { content: 'Hi there!' } }] });
  });

  it('send — should include system prompt in messages', () => {
    service.send([], 'Hello', 'System prompt here').subscribe();
    const req = http.expectOne(GROQ_URL);
    const messages = req.request.body.messages;
    expect(messages[0].role).toBe('system');
    expect(messages[0].content).toBe('System prompt here');
    req.flush({ choices: [{ message: { content: 'Hi!' } }] });
  });

  it('send — should include user message as last message', () => {
    service.send([], 'What is the check-in time?', 'System').subscribe();
    const req = http.expectOne(GROQ_URL);
    const messages = req.request.body.messages;
    const last = messages[messages.length - 1];
    expect(last.role).toBe('user');
    expect(last.content).toBe('What is the check-in time?');
    req.flush({ choices: [{ message: { content: 'Check-in is at 2 PM.' } }] });
  });

  it('send — should map model messages to assistant role', () => {
    const history: ChatMessage[] = [{ role: 'model', text: 'Hello!' }];
    service.send(history, 'Thanks', 'System').subscribe();
    const req = http.expectOne(GROQ_URL);
    const messages = req.request.body.messages;
    const assistantMsg = messages.find((m: any) => m.content === 'Hello!');
    expect(assistantMsg?.role).toBe('assistant');
    req.flush({ choices: [{ message: { content: 'You are welcome!' } }] });
  });

  it('send — should return the reply text from choices', () => {
    let result = '';
    service.send([], 'Hello', 'System').subscribe(r => result = r);
    const req = http.expectOne(GROQ_URL);
    req.flush({ choices: [{ message: { content: 'Hi there!' } }] });
    expect(result).toBe('Hi there!');
  });

  it('send — should return fallback message when choices is empty', () => {
    let result = '';
    service.send([], 'Hello', 'System').subscribe(r => result = r);
    const req = http.expectOne(GROQ_URL);
    req.flush({ choices: [] });
    expect(result).toContain('Sorry');
  });

  it('send — should use llama-3.1-8b-instant model', () => {
    service.send([], 'Hello', 'System').subscribe();
    const req = http.expectOne(GROQ_URL);
    expect(req.request.body.model).toBe('llama-3.1-8b-instant');
    req.flush({ choices: [{ message: { content: 'Hi!' } }] });
  });

  it('send — should limit history to last 6 messages', () => {
    const history: ChatMessage[] = Array.from({ length: 10 }, (_, i) => ({
      role: i % 2 === 0 ? 'user' : 'model' as 'user' | 'model',
      text: `msg ${i}`
    }));
    service.send(history, 'New message', 'System').subscribe();
    const req = http.expectOne(GROQ_URL);
    // system + 6 history + 1 user = 8 messages max
    expect(req.request.body.messages.length).toBeLessThanOrEqual(8);
    req.flush({ choices: [{ message: { content: 'Reply' } }] });
  });
});
