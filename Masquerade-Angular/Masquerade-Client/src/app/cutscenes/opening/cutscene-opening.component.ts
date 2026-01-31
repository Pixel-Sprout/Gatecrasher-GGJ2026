import { CommonModule } from "@angular/common";
import { Component, OnInit, inject, OnDestroy, signal } from "@angular/core";
import { AppStateService } from '../../services/app-state.service';
import { GameState } from '../../types/game-state.enum';

@Component({
  selector: 'app-cutscene-opening',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cutscene-opening.component.html',
  styleUrl: './cutscene-opening.component.scss',
})
export class CutsceneOpeningComponent implements OnInit, OnDestroy {
  private appState = inject(AppStateService);

  // animation timing (ms)
  private perImageDuration = 2000; // time between image starts
  private fadeDuration = 600; // fade-in duration
 
  private timers: any[] = [];

  public images = signal<string[]>([]);
  public containerFading = false;

  public ngOnInit(): void {
    this.loadFirstImages().then(() => this.startSequence());
  }

  ngOnDestroy(): void {
    this.timers.forEach(t => clearTimeout(t));
    this.timers = [];
  }

  private tryLoadImage(url: string): Promise<boolean> {
    return new Promise(resolve => {
      const img = new Image();
      img.onload = () => resolve(true);
      img.onerror = () => resolve(false);
      img.src = url;
    });
  }

  // Attempt to load numbered images in order until we have at least 3 or reach maxCheck
  private async loadFirstImages(): Promise<void> {
    const candidates = ['01-king-invitation.png', '02-the-witch-listens.png', '03-witch-plotting.png', '04-to-the-party.png'];
    const base = 'imgs/opening/';
    const foundImages: string[] = [];

    
    for (const candidate of candidates) {
      const fullPath = `${base}${candidate}`;
      // skip if already found
      if (foundImages.includes(fullPath)) continue;
      // try load
      // eslint-disable-next-line no-await-in-loop
      const ok = await this.tryLoadImage(fullPath);
      if (ok) {
        foundImages.push(fullPath);
      }
    }

    // fallback: if no images found, leave images empty (component still renders gracefully)
    this.images.set(foundImages);
  }

  private startSequence(): void {
 

    // schedule container fade-out and state change after third image finishes showing
    const imagesToShow = this.images().length;
    const lastImageStart = (imagesToShow - 1) * this.perImageDuration;
    const totalBeforeFadeOut = lastImageStart + this.perImageDuration; // allow last image to be visible for one slot

    // Fade out container slightly after the sequence
    const fadeOutAt = totalBeforeFadeOut;

    // After the sequence finishes, wait 2s then change app state to lobby
    this.timers.push(setTimeout(() => {
      this.appState.setState(GameState.LOBBY, null);
    }, fadeOutAt + 2000));
  }

  // helpers used in template
  public animationDelay(index: number): string {
    return `${index * this.perImageDuration}ms`;
  }

  public animationDuration(): string {
    return `${this.fadeDuration}ms`;
  }
}