import {Inject, Injectable} from '@angular/core';
import { WINDOW } from '../window.provider';

@Injectable({ providedIn: 'root' })
export class EndpointLocator{

  private hubEndpoint:string = 'http://localhost:5000'
  private apiEndpoint:string = 'http://localhost:5000';
  private joinUrlBase:string = 'http://localhost:4200';

  constructor(@Inject(WINDOW) private window: Window) {
    if(window.location.host != "localhost:4200"){
      this.hubEndpoint = `https://maska.mufinek.pl`;
      this.apiEndpoint = `https://maska.mufinek.pl`;
      this.joinUrlBase = `https://maska.mufinek.pl`;
    }
  }
  getSignalRHubEndpoint():string{
    return this.hubEndpoint
  }

  getHttpApiEndpoint():string{
    return this.apiEndpoint;
  }

  getRoomJoinUrl(gameId:string):string{
    return `${this.joinUrlBase}/?${gameId}`;
  }
}
