import { CommonModule } from "@angular/common";
import { Component, OnInit, inject, OnDestroy, signal } from "@angular/core";
import { AppStateService } from '../../services/app-state.service';
import { GameState } from '../../types/game-state.enum';
import { GameHubService } from "../../services/gamehub.service";

@Component({
  selector: 'app-cutscene-the-choice',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cutscene-the-choice.component.html',
  styleUrl: './cutscene-the-choice.component.scss',
})
export class CutsceneTheChoiceComponent implements OnInit, OnDestroy {
  private appState = inject(AppStateService);
  private svc = inject(GameHubService);

  // animation timing (ms)
  private perImageDuration = 1500; // time between image starts
  private fadeDuration = 400; // fade-in duration
  private timers: any[] = [];

  public images = signal<string[]>([]);
  public containerFading = false;

  public ngOnInit(): void {
    this.loadFirstImages().then(() => this.startSequence());

    this.svc.onReceivePhaseChanged().subscribe(([phase, message]) =>
      setTimeout(() => {
        this.appState.setState(phase as GameState, message);
      }, 800)
    );
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
    const goodEnding = ['good-1-you-chose-witch.jpg', 'good-2-witch-captured.jpg', 'good-3-witch-lost.jpg'];
    const badEnding = ['bad-1-you-chose-player.jpg', 'bad-2-witch-attacks.jpg', 'bad-3-king-dead.jpg'];
    
    for (const candidate of (this.appState.cutsceneMessageSignal().playAlternativeCutscene ? badEnding : goodEnding)) {
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
      this.svc.ready();
    }, fadeOutAt + 1000));
  }

  // helpers used in template
  public animationDelay(index: number): string {
    return `${index * this.perImageDuration}ms`;
  }

  public animationDuration(): string {
    return `${this.fadeDuration}ms`;
  }
}
