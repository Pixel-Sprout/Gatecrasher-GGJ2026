import { Component, ViewChild, ElementRef, AfterViewInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-mask-creator',
  imports: [RouterOutlet, CommonModule, FormsModule],
  templateUrl: './mask-creator.component.html',
  styleUrl: './mask-creator.component.scss',
  standalone: true
})
export class MaskCreatorComponent implements AfterViewInit {
  @ViewChild('maskCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  // Drawing properties
  brushColor: string = '#000000';
  brushSize: number = 5;
  showInstructions: boolean = true;
  canvasHasDrawing: boolean = false;
  
  private canvas!: HTMLCanvasElement;
  private context!: CanvasRenderingContext2D;
  private isDrawing: boolean = false;
  private lastX: number = 0;
  private lastY: number = 0;

  private router = inject(Router);

  ngAfterViewInit(): void {
    this.initializeCanvas();
  }

  private initializeCanvas(): void {
    this.canvas = document.getElementById('maskCanvas') as HTMLCanvasElement;
    
    if (this.canvas) {
      // Set canvas size to fill its container
      const wrapper = this.canvas.parentElement as HTMLElement;
      this.canvas.width = wrapper.clientWidth - 40; // Account for padding
      this.canvas.height = wrapper.clientHeight - 60; // Account for padding and instructions
      
      this.context = this.canvas.getContext('2d')!;
      
      // Set white background
      this.context.fillStyle = '#ffffff';
      this.context.fillRect(0, 0, this.canvas.width, this.canvas.height);
      
      // Show instructions initially
      this.showInstructions = true;
      this.canvasHasDrawing = false;
    }
  }

  startDrawing(event: MouseEvent): void {
    if (!this.canvas) return;
    
    // Hide instructions when user starts drawing
    this.showInstructions = false;
    this.canvasHasDrawing = true;
    
    this.isDrawing = true;
    const rect = this.canvas.getBoundingClientRect();
    this.lastX = event.clientX - rect.left;
    this.lastY = event.clientY - rect.top;
  }

  draw(event: MouseEvent): void {
    if (!this.isDrawing || !this.context) return;

    const rect = this.canvas.getBoundingClientRect();
    const currentX = event.clientX - rect.left;
    const currentY = event.clientY - rect.top;

    // Draw line from last position to current position
    this.context.strokeStyle = this.brushColor;
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
  }

  clearCanvas(): void {
    if (!this.context) return;
    
    this.context.fillStyle = '#ffffff';
    this.context.fillRect(0, 0, this.canvas.width, this.canvas.height);
    
    // Show instructions again after clearing
    this.showInstructions = true;
    this.canvasHasDrawing = false;
  }

  onReady(): void {
    // Get canvas data as image
    const imageData = this.canvas.toDataURL('image/png');
    console.log('Mask drawing saved:', imageData);
    // TODO: Send to backend or store in service
    
    // Redirect to mask-comparison view
    this.router.navigate(['/mask-comparison']);
  }

  onCancel(): void {
    // Navigate back to lobby
    this.router.navigate(['/lobby']);
  }
}