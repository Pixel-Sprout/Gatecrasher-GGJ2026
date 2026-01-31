import { CommonModule } from "@angular/common";
import { Component, OnInit, inject, OnDestroy, signal } from "@angular/core";
import { AppStateService } from '../../services/app-state.service';
import { GameState } from '../../types/game-state.enum';

@Component({
  selector: 'app-cutscene-make-the-mask',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cutscene-make-the-mask.component.html',
  styleUrl: './cutscene-make-the-mask.component.scss',
})
export class CutsceneMakeTheMaskComponent implements OnInit, OnDestroy {
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
    console.log('loading', url);
    return new Promise(resolve => {
      const img = new Image();
      img.onload = () => resolve(true);
      img.onerror = () => resolve(false);
      img.src = url;
    });
  }

  // Attempt to load the two predefined images for this cutscene
  private async loadFirstImages(): Promise<void> {
    const base = 'imgs/make-the-mask/';
    const candidates = ['01-people-preparing-masks.png', '02-witch-preparing-mask.png'];
    const foundImages: string[] = [];

    for (const candidate of candidates) {
      const fullPath = `${base}${candidate}`;
      if (foundImages.includes(fullPath)) continue;
      // eslint-disable-next-line no-await-in-loop
      const ok = await this.tryLoadImage(fullPath);
      if (ok) foundImages.push(fullPath);
    }

    this.images.set(foundImages);
  }

  private startSequence(): void {
    // schedule container fade-out and state change after third image finishes showing
    
    const imagesToShow = Math.min(3, this.images().length);
    const lastImageStart = (imagesToShow - 1) * this.perImageDuration;
    const totalBeforeFadeOut = lastImageStart + this.perImageDuration; // allow last image to be visible for one slot

    // Fade out container slightly after the sequence
    const fadeOutAt = totalBeforeFadeOut;
    this.timers.push(setTimeout(() => {
      this.containerFading = true;
    }, fadeOutAt));

    // After the sequence finishes, wait 2s then change app state to MASK_DRAW
    this.timers.push(setTimeout(() => {
      this.appState.setState(GameState.MASK_DRAW, null);
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