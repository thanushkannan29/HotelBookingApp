import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ChatMessage {
  role: 'user' | 'model';
  text: string;
}

interface GroqResponse {
  choices: { message: { content: string } }[];
}

@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.groqApiUrl;

  send(history: ChatMessage[], userMessage: string, systemPrompt: string): Observable<string> {
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${environment.groqApiKey}`,
      'Content-Type': 'application/json'
    });

    // Keep last 6 messages only to stay within token limits
    const recentHistory = history.slice(-6);

    const messages = [
      { role: 'system', content: systemPrompt },
      ...recentHistory.map(m => ({
        role: m.role === 'model' ? 'assistant' : 'user',
        content: m.text
      })),
      { role: 'user', content: userMessage }
    ];

    const body = {
      model: 'llama-3.1-8b-instant',
      messages,
      max_tokens: 512,
      temperature: 0.7
    };

    return this.http.post<GroqResponse>(this.apiUrl, body, { headers }).pipe(
      map(res => res.choices?.[0]?.message?.content
        ?? 'Sorry, I could not get a response. Please try again.')
    );
  }
}
