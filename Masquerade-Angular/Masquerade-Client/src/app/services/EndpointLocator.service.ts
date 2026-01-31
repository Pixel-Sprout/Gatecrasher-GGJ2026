import {Inject, Injectable} from '@angular/core';
import { WINDOW } from '../window.provider';

@Injectable({ providedIn: 'root' })
export class EndpointLocator{

  private hubEndpoint:string = 'https://maska.mufinek.pl'
  private apiEndpoint:string = 'https://maska.mufinek.pl';

  constructor(@Inject(WINDOW) private window: Window) {
    if(window.location.host == "localhost:4200"){
      this.hubEndpoint = 'http://localhost:5000';
      this.apiEndpoint = 'http://localhost:5000';
      console.info('Using development enspoints');
    }
  }
  getSignalRHubEndpoint():string{
    return this.hubEndpoint
  }

  getHttpApiEndpoint():string{
    return this.apiEndpoint;
  }
}
