import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ChatMessage {
  role: 'user' | 'model';
  text: string;
}

interface GeminiResponse {
  candidates: { content: { parts: { text: string }[] } }[];
}

@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private http = inject(HttpClient);

  private get apiUrl(): string {
    return `https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key=${environment.geminiApiKey}`;
  }

  send(history: ChatMessage[], userMessage: string, systemPrompt: string): Observable<string> {
    const contents = [
      { role: 'user', parts: [{ text: systemPrompt }] },
      { role: 'model', parts: [{ text: 'Understood! I am the StayHub AI assistant. I am ready to help.' }] },
      ...history.map(m => ({ role: m.role, parts: [{ text: m.text }] })),
      { role: 'user', parts: [{ text: userMessage }] }
    ];

    return this.http.post<GeminiResponse>(this.apiUrl, { contents }).pipe(
      map(res => res.candidates?.[0]?.content?.parts?.[0]?.text ?? 'Sorry, I could not get a response. Please try again.')
    );
  }
}
