import { Component, ViewChild, ElementRef, AfterViewInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface FeatureSection {
  name: string;
  description: string;
}

@Component({
  selector: 'app-mask-creator',
  imports: [RouterOutlet, CommonModule, FormsModule],
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
  featureSections: FeatureSection[] = [
    { name: 'Oczy', description: 'Duże' },
    { name: 'Usta', description: 'Szerokie' },
    { name: 'Nos', description: 'Mały' },
    { name: 'Zarost', description: 'Gęsty' },
    { name: 'Uszy', description: 'Szpiczaste' }
  ];
  
  private canvas!: HTMLCanvasElement;
  private context!: CanvasRenderingContext2D;
  private cssWidth: number = 0;
  private cssHeight: number = 0;
  isEraser: boolean = false;
  private isDrawing: boolean = false;
  private lastX: number = 0;
  private lastY: number = 0;

  private router = inject(Router);

  ngAfterViewInit(): void {
    this.loadFeatureSectionsFromState();
    this.initializeCanvas();
    this.updateBrushPreview();
  }

  private loadFeatureSectionsFromState(): void {
    try {
      const state = (history && (history as any).state) || {};
      if (state && state.featureSections && Array.isArray(state.featureSections)) {
        this.featureSections = state.featureSections as FeatureSection[];
        console.log('Loaded featureSections from navigation state', this.featureSections);
      }
    } catch (e) {
      // ignore
    }
  }

  private initializeCanvas(): void {
    // Use the ViewChild canvas element
    this.canvas = this.canvasRef?.nativeElement;

    if (!this.canvas) return;

    // Get displayed (CSS) size and scale the internal pixel buffer for devicePixelRatio
    const rect = this.canvas.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;

    // remember logical CSS size for drawing coordinates and clearing
    this.cssWidth = Math.round(rect.width);
    this.cssHeight = Math.round(rect.height);

    // ensure the style size matches the computed size (keeps layout stable)
    this.canvas.style.width = `${this.cssWidth}px`;
    this.canvas.style.height = `${this.cssHeight}px`;

    // set actual canvas pixel size and scale context so we can draw using CSS pixels
    this.canvas.width = Math.round(this.cssWidth * dpr);
    this.canvas.height = Math.round(this.cssHeight * dpr);

    this.context = this.canvas.getContext('2d')!;
    // scale drawing operations to map CSS pixels to device pixels
    this.context.setTransform(dpr, 0, 0, dpr, 0, 0);
    // ensure default composite mode
    this.context.globalCompositeOperation = 'source-over';

    // Set white background (use CSS pixel sizes because context is scaled)
    this.context.fillStyle = '#ffffff';
    this.context.fillRect(0, 0, this.cssWidth, this.cssHeight);

    // Show instructions initially
    this.showInstructions = true;
    this.canvasHasDrawing = false;
  }

  startDrawing(event: MouseEvent): void {
    if (!this.canvas) return;
    
    // Hide instructions when user starts drawing
    this.showInstructions = false;
    this.canvasHasDrawing = true;
    
    this.isDrawing = true;
    const rect = this.canvas.getBoundingClientRect();
    // Use CSS pixel coordinates (context is scaled to DPR)
    this.lastX = event.clientX - rect.left;
    this.lastY = event.clientY - rect.top;
  }

  draw(event: MouseEvent): void {
    if (!this.isDrawing || !this.context) return;

    const rect = this.canvas.getBoundingClientRect();
    const currentX = event.clientX - rect.left;
    const currentY = event.clientY - rect.top;

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

  onReady(): void {
    // Get canvas data as image
    const imageData = this.canvas.toDataURL('image/png');
    console.log('Mask drawing saved:', imageData);

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

    // Redirect to mask-comparison view
    this.router.navigate(['/mask-comparison']);
  }

  onCancel(): void {
    // Navigate back to lobby
    this.router.navigate(['/lobby']);
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