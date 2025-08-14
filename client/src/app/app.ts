import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { StoryListComponent } from './components/story-list/story-list.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, StoryListComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('Hacker News Reader');
}
