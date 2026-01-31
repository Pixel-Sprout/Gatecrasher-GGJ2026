import { CommonModule } from "@angular/common";
import { Component, OnInit, inject, OnDestroy, signal } from "@angular/core";
import { AppStateService } from '../../services/app-state.service';
import { GameState } from '../../types/game-state.enum';

@Component({
  selector: 'app-cutscene-the-choice',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cutscene-the-choice.component.html',
  styleUrl: './cutscene-the-choice.component.scss',
})
export class CutsceneTheChoiceComponent implements OnInit, OnDestroy {
  private appState = inject(AppStateService);

  // animation timing (ms)
  private perImageDuration = 2200; // time between image starts
  private fadeDuration = 800; // fade-in duration
  private fadeOutDuration = 900; // container fade-out duration
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

  // Attempt to load numbered images in order
  private async loadFirstImages(): Promise<void> {
    const base = 'imgs/the-choice/';
    const foundImages: string[] = [];

    // TODO: use proper ending images based on players' choice / server data
    const goodEnding = ['good-1-you-chose-witch.png', 'good-2-witch-captured.png', 'good-3-witch-lost.png'];
    const badEnding = ['bad-1-you-chose-player.png', 'bad-2-witch-attacks.png', 'bad-3-king-dead.png'];
    
    for (const candidate of goodEnding) {
      const fullPath = `${base}${candidate}`;
      // skip if already found
      if (foundImages.includes(fullPath)) continue;
      // try load
      // eslint-disable-next-line no-await-in-loop
      const ok = await this.tryLoadImage(fullPath);
      if (ok) foundImages.push(fullPath);
    }

    // fallback: if no images found, leave images empty (component still renders gracefully)
    this.images.set(foundImages);
  }

  private startSequence(): void {
    const imgs = this.images();
    // schedule container fade-out and state change after third image finishes showing
    const imagesToShow = Math.min(3, imgs.length);
    const lastImageStart = (imagesToShow - 1) * this.perImageDuration;
    const totalBeforeFadeOut = lastImageStart + this.perImageDuration; // allow last image to be visible for one slot

    // Fade out container slightly after the sequence
    const fadeOutAt = totalBeforeFadeOut;
    this.timers.push(setTimeout(() => {
      this.containerFading = true;
    }, fadeOutAt));

    // After the sequence finishes, wait 2s then change app state to ballroom
    this.timers.push(setTimeout(() => {
      this.appState.setState(GameState.SCORING, null);
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