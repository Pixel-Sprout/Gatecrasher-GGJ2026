import {inject, Injectable} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { EndpointLocator } from './EndpointLocator.service';

@Injectable({ providedIn: 'root' })
export class HttpApiService {
  private locator: EndpointLocator = inject(EndpointLocator);

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

    const url = `${this.locator.getHttpApiEndpoint()}/Game/${encodeURIComponent(gameId)}/${encodeURIComponent(playerId)}/drawing`;
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(image)
    });
	}
}
