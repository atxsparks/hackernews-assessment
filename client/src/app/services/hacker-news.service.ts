import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Story, PaginatedStories, TimeAgoResult } from '../interfaces/story.interface';

@Injectable({
  providedIn: 'root'
})
export class HackerNewsService {
  private readonly baseUrl = 'http://localhost:5248/api/stories';

  constructor(private readonly http: HttpClient) {}

  getNewStories(page: number = 1, pageSize: number = 20): Observable<PaginatedStories> {
    const params = { page: page.toString(), pageSize: pageSize.toString() };
    return this.http.get<PaginatedStories>(`${this.baseUrl}/newest`, { params });
  }

  getStoryById(id: number): Observable<Story> {
    return this.http.get<Story>(`${this.baseUrl}/${id}`);
  }

  searchStories(query: string, limit: number = 50): Observable<Story[]> {
    if (!query.trim()) {
      return this.getNewStories(1, limit).pipe(map(result => result.stories));
    }
    const params = { q: query.trim(), limit: limit.toString() };
    return this.http.get<Story[]>(`${this.baseUrl}/search`, { params });
  }

  formatTimeAgo(timestamp: number): string {
    const timeAgo = this.calculateTimeAgo(timestamp);
    return timeAgo.unit === 'now' ? 'Just now' : 
      `${timeAgo.value} ${timeAgo.unit}${timeAgo.value > 1 ? 's' : ''} ago`;
  }

  hasValidUrl(story: Story): boolean {
    return Boolean(story.url?.trim());
  }

  getStoryUrl(story: Story): string {
    return this.hasValidUrl(story) ? 
      story.url! : 
      `https://news.ycombinator.com/item?id=${story.id}`;
  }



  private calculateTimeAgo(timestamp: number): TimeAgoResult {
    const difference = Date.now() - (timestamp * 1000);
    const seconds = Math.floor(difference / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    
    if (days > 0) return { value: days, unit: 'day' };
    if (hours > 0) return { value: hours, unit: 'hour' };
    if (minutes > 0) return { value: minutes, unit: 'minute' };
    return { value: 0, unit: 'now' };
  }
}
