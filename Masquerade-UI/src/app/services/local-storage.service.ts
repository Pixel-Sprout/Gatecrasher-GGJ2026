import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';

interface InMemoryDict{
  [key: string]: string;
}

@Injectable({
  providedIn: 'root',
})
export class LocalStorageService {
  private readonly USERTOKEN_KEY = 'userToken';

  private inMemoryStorage: InMemoryDict = {};
  private _userTokenSource:BehaviorSubject<string>;
  public UserToken$:Observable<string>

  constructor() {
    let token = this.getValue(this.USERTOKEN_KEY);
    if(!token){
      token = crypto.randomUUID();
      this.setValue(this.USERTOKEN_KEY, token);
    }
    this._userTokenSource = new BehaviorSubject<string>(token);
    this.UserToken$ = this._userTokenSource.asObservable();
  }

  private getValue(key: string): string | null {
    try {
      if (typeof window === 'undefined' || typeof localStorage === 'undefined') {
        return this.inMemoryStorage[key] ;
      }
      return localStorage.getItem(key);
    } catch (e) {
      console.error('Error getting data from storage:', e);
      return null;
    }
  }

  private setValue(key: string, value: string): void {
    try {
      if (typeof window === 'undefined' || typeof localStorage === 'undefined') {
        this.inMemoryStorage[key] = value;
      }
      else{
       localStorage.setItem(key, value);
       }
    } catch (e) {
      console.error('Error storing data to storage:', e);
      return ;
    }
  }

  public setUserToken(value: string) {
    this.setValue(this.USERTOKEN_KEY, value);
  }

}
