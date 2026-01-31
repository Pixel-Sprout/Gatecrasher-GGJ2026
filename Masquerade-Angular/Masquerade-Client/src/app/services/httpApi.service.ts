import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class HttpApiService {
  private baseUrl = 'http://localhost:5000'; //To Adjust url use procy.conf.json during development

	constructor(private http: HttpClient) {
	}

	/**
	 * Wysyła POST do kontrolera GameController: /Game/{gameId}/{playerId}/drawing
	 * @param playerId - id gracza / playerId (w ścieżce)
	 * @param text - treść (body) wysyłana jako JSON-string
	 * @param gameId - id gry
	 */
	async postDrawing(playerId: string, gameId: string, image: string) {
    if (!playerId) {
      throw new Error('playerId is required');
    }
    if (!gameId) {
      throw new Error('gameId is required');
    }

    const url = `${this.baseUrl}/Game/${encodeURIComponent(gameId)}/${encodeURIComponent(playerId)}/drawing`;
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(image)
    });
	}
}
