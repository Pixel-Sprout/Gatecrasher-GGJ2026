import { Component, ViewChild, ElementRef, AfterViewInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { GameHubService } from '../services/gamehub.service';
import { HttpApiService } from '../services/httpApi.service';
import { AppStateService } from '../services/app-state.service';
import { GameState } from '../types/game-state.enum';

interface FeatureSection {
  name: string;
  description: string;
}

@Component({
  selector: 'app-mask-creator',
  imports: [CommonModule, FormsModule],
  templateUrl: './mask-creator.component.html',
  styleUrl: './mask-creator.component.scss',
  standalone: true
})
export class MaskCreatorComponent implements AfterViewInit {
  @ViewChild('maskCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('brushPreviewCanvas') brushPreviewCanvasRef!: ElementRef<HTMLCanvasElement>;

  // Drawing properties
  brushColor: string = '#000000';
  brushSize: number = 5;
  showInstructions: boolean = true;
  canvasHasDrawing: boolean = false;

  // Features to draw
  featureSections: string[] = [];

  private canvas!: HTMLCanvasElement;
  private context!: CanvasRenderingContext2D;
  private cssWidth: number = 0;
  private cssHeight: number = 0;
  isEraser: boolean = false;
  private isDrawing: boolean = false;
  private lastX: number = 0;
  private lastY: number = 0;
  public isBadPlayer = signal<boolean>(false);

  private svc = inject(GameHubService);
  private api = inject(HttpApiService);
  private appState = inject(AppStateService);

  ngAfterViewInit(): void {
    this.loadFeatureSectionsFromState();
    this.initializeCanvas();
    this.updateBrushPreview();

    this.svc.onReceivePhaseChanged().subscribe(([phase, message]) =>
      setTimeout(() => {
        this.appState.setState(phase as GameState, message);
      }, 800)
    );
  }

  private loadFeatureSectionsFromState(): void {
    try {
      var message = this.appState.drawingMessageSignal();
      if (message) { 
        if (message.maskDescriptions && Array.isArray(message.maskDescriptions)) {
          this.featureSections = message.maskDescriptions;
          console.log('Loaded featureSections from navigation state', this.featureSections);
        }
        if (message.isPlayerEvil !== undefined)
        {
          this.isBadPlayer.set(message.isPlayerEvil);
          console.log('isBadPlayer set to', this.isBadPlayer());
        }
      }
    } catch (e) {
      // ignore
    }
  }

  private initializeCanvas(): void {
    // Use the ViewChild canvas element
    this.canvas = this.canvasRef?.nativeElement;

    if (!this.canvas) return;

    // Determine canvas size based on screen width
    const screenWidth = window.innerWidth;
    const canvasSize = screenWidth < 768 ? 310 : 440;

    // Get devicePixelRatio for high-DPI displays
    const dpr = window.devicePixelRatio || 1;

    // Set logical CSS size
    this.cssWidth = canvasSize;
    this.cssHeight = canvasSize;

    // Set canvas element CSS display size
    this.canvas.style.width = `${canvasSize}px`;
    this.canvas.style.height = `${canvasSize}px`;

    // Set actual canvas pixel buffer size (scale by DPR)
    this.canvas.width = canvasSize * dpr;
    this.canvas.height = canvasSize * dpr;

    this.context = this.canvas.getContext('2d')!;
    // Scale context to handle DPR so drawing coordinates match CSS pixels
    this.context.scale(dpr, dpr);
    // ensure default composite mode
    this.context.globalCompositeOperation = 'source-over';

    // Set white background (use CSS pixel sizes)
    this.context.fillStyle = '#ffffff';
    this.context.fillRect(0, 0, this.cssWidth, this.cssHeight);

    // Show instructions initially
    this.showInstructions = true;
    this.canvasHasDrawing = false;
  }

  touchStart(event: TouchEvent): void {
    this._startDrawing(event.touches[0].clientX, event.touches[0].clientY);
  }

  touchEnd(event: TouchEvent): void {
    event.preventDefault();
    console.log("touch end", event);
  }

  touchMove(event: TouchEvent): void {
    event.preventDefault();
    this._draw(event.touches[0].clientX, event.touches[0].clientY);
    console.log("touch move", event);
  }

  startDrawing(event: MouseEvent): void {
    this._startDrawing(event.clientX, event.clientY);
    /*if (!this.canvas) return;

    // Hide instructions when user starts drawing
    this.showInstructions = false;
    this.canvasHasDrawing = true;

    this.isDrawing = true;
    const rect = this.canvas.getBoundingClientRect();
    // Use CSS pixel coordinates (context is scaled to DPR)
    this.lastX = event.clientX - rect.left;
    this.lastY = event.clientY - rect.top;*/
  }

  _startDrawing(clientX:number, clientY: number){
    if (!this.canvas) return;

    // Hide instructions when user starts drawing
    this.showInstructions = false;
    this.canvasHasDrawing = true;

    this.isDrawing = true;
    const rect = this.canvas.getBoundingClientRect();
    // Use CSS pixel coordinates (context is scaled to DPR)
    this.lastX = clientX - rect.left;
    this.lastY = clientY - rect.top;
  }

  draw(event: MouseEvent): void {
    this._draw(event.clientX, event.clientY)
  }
  _draw(clientX:number, clientY: number): void {
    if (!this.isDrawing || !this.context) return;

    const rect = this.canvas.getBoundingClientRect();
    const currentX = clientX - rect.left;
    const currentY = clientY - rect.top;

    // Draw line from last position to current position
    // Use eraser by switching composite operation; destination-out erases pixels
    if (this.isEraser) {
      this.context.globalCompositeOperation = 'destination-out';
      this.context.strokeStyle = 'rgba(0,0,0,1)';
    } else {
      this.context.globalCompositeOperation = 'source-over';
      this.context.strokeStyle = this.brushColor;
    }
    this.context.lineWidth = this.brushSize;
    this.context.lineCap = 'round';
    this.context.lineJoin = 'round';

    this.context.beginPath();
    this.context.moveTo(this.lastX, this.lastY);
    this.context.lineTo(currentX, currentY);
    this.context.stroke();

    this.lastX = currentX;
    this.lastY = currentY;
  }

  stopDrawing(): void {
    this.isDrawing = false;
    // restore default composite mode after finishing
    if (this.context) this.context.globalCompositeOperation = 'source-over';
  }

  clearCanvas(): void {
    if (!this.context) return;
    this.context.fillStyle = '#ffffff';
    // clear using CSS pixel dimensions (context is already scaled)
    this.context.fillRect(0, 0, this.cssWidth || this.canvas.width, this.cssHeight || this.canvas.height);

    // Show instructions again after clearing
    this.showInstructions = true;
    this.canvasHasDrawing = false;
  }

  async onReady(): Promise<void> {
    // Get canvas data as image
    const imageData = this.canvas.toDataURL('image/png');
    console.log('Mask drawing saved:', imageData);

    await this.api.postDrawing(this.svc.playerId, this.svc.gameId, imageData);

    // Save to localStorage as temporary transport to mask-comparison
    try {
      const key = 'masquerade_masks';
      const stored = JSON.parse(localStorage.getItem(key) || '[]');
      const myMask = {
        id: 'player1',
        playerName: 'You',
        playerRole: 'Mask Maker',
        imageData
      };

      // replace existing entry for this player if present
      const idx = stored.findIndex((m: any) => m.id === myMask.id);
      if (idx >= 0) {
        stored[idx] = myMask;
      } else {
        stored.push(myMask);
      }
      localStorage.setItem(key, JSON.stringify(stored));
    } catch (e) {
      console.warn('Could not save mask to localStorage', e);
    }

    // Navigate to mask-comparison
    this.svc.ready();
  }

  toggleEraser(): void {
    this.isEraser = !this.isEraser;
    this.updateBrushPreview();
  }

  setEraser(value: boolean): void {
    if (this.isEraser === value) return;
    this.isEraser = value;
    this.updateBrushPreview();
  }

  updateBrushPreview(): void {
    const previewCanvas = this.brushPreviewCanvasRef?.nativeElement;
    if (!previewCanvas) return;
    const dpr = window.devicePixelRatio || 1;
    // Use client size (CSS pixels) to avoid changing layout by repeatedly setting style
    const cw = previewCanvas.clientWidth || previewCanvas.width || 60;
    const ch = previewCanvas.clientHeight || previewCanvas.height || 60;

    // set internal pixel buffer for crisp rendering on HiDPI displays
    previewCanvas.width = Math.round(cw * dpr);
    previewCanvas.height = Math.round(ch * dpr);

    const previewContext = previewCanvas.getContext('2d');
    if (!previewContext) return;
    previewContext.setTransform(dpr, 0, 0, dpr, 0, 0);

    // Clear the preview canvas
    previewContext.clearRect(0, 0, cw, ch);
    previewContext.fillStyle = '#ffffff';
    previewContext.fillRect(0, 0, cw, ch);

    // Draw a centered circular preview of the brush
    const cx = cw / 2;
    const cy = ch / 2;
    const radius = Math.max(1, this.brushSize / 2);
    if (this.isEraser) {
      previewContext.strokeStyle = '#7f8c8d';
      previewContext.lineWidth = Math.max(2, Math.min(6, Math.round(this.brushSize / 3)));
      previewContext.beginPath();
      previewContext.arc(cx, cy, radius, 0, Math.PI * 2);
      previewContext.stroke();
    } else {
      previewContext.fillStyle = this.brushColor;
      previewContext.beginPath();
      previewContext.arc(cx, cy, radius, 0, Math.PI * 2);
      previewContext.fill();
    }
  }
}
