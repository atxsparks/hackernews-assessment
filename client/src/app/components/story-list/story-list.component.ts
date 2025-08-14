import { Component, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HackerNewsService } from '../../services/hacker-news.service';
import { Story, PaginatedStories } from '../../interfaces/story.interface';
import { debounceTime, distinctUntilChanged, Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-story-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './story-list.component.html',
  styleUrl: './story-list.component.scss'
})
export class StoryListComponent implements OnInit, OnDestroy {
  readonly stories = signal<Story[]>([]);
  readonly filteredStories = signal<Story[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly currentPage = signal(1);
  readonly totalPages = signal(0);
  readonly totalCount = signal(0);
  readonly searchQuery = signal('');
  readonly pageSize = 20;

  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(private readonly hackerNewsService: HackerNewsService) {
    this.initializeSearch();
  }

  ngOnInit(): void {
    this.loadStories();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadStories(page: number = 1): void {
    this.setLoadingState(true);
    
    this.hackerNewsService.getNewStories(page, this.pageSize)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: PaginatedStories) => this.handleStoriesSuccess(data),
        error: () => this.handleStoriesError()
      });
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchSubject.next(input.value);
  }

  goToPage(page: number): void {
    if (this.isValidPageTransition(page)) {
      this.loadStories(page);
    }
  }

  nextPage(): void {
    this.goToPage(this.currentPage() + 1);
  }

  previousPage(): void {
    this.goToPage(this.currentPage() - 1);
  }

  getTimeAgo(timestamp: number): string {
    return this.hackerNewsService.formatTimeAgo(timestamp);
  }

  hasValidUrl(story: Story): boolean {
    return this.hackerNewsService.hasValidUrl(story);
  }

  getStoryUrl(story: Story): string {
    return this.hackerNewsService.getStoryUrl(story);
  }

  getPageNumbers(): number[] {
    return this.calculatePageNumbers();
  }

  private initializeSearch(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(query => {
      this.searchQuery.set(query);
      this.filterStories();
    });
  }

  private setLoadingState(loading: boolean): void {
    this.loading.set(loading);
    this.error.set(null);
  }

  private handleStoriesSuccess(data: PaginatedStories): void {
    this.stories.set(data.stories);
    this.currentPage.set(data.currentPage);
    this.totalPages.set(data.totalPages);
    this.totalCount.set(data.totalCount);
    this.filterStories();
    this.loading.set(false);
  }

  private handleStoriesError(): void {
    this.error.set('Failed to load stories. Please try again.');
    this.loading.set(false);
  }

  filterStories(): void {
    const query = this.searchQuery();
    if (!query.trim()) {
      this.filteredStories.set(this.stories());
      return;
    }

    this.hackerNewsService.searchStories(query, 50)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stories) => this.filteredStories.set(stories),
        error: () => this.filteredStories.set([])
      });
  }

  private isValidPageTransition(page: number): boolean {
    return page >= 1 && 
           page <= this.totalPages() && 
           page !== this.currentPage();
  }

  private calculatePageNumbers(): number[] {
    const current = this.currentPage();
    const total = this.totalPages();
    const pages: number[] = [];
    const ELLIPSIS = -1;
    
    if (current > 3) {
      pages.push(1);
      if (current > 4) pages.push(ELLIPSIS);
    }
    
    for (let i = Math.max(1, current - 2); i <= Math.min(total, current + 2); i++) {
      pages.push(i);
    }
    
    if (current < total - 2) {
      if (current < total - 3) pages.push(ELLIPSIS);
      pages.push(total);
    }
    
    return pages;
  }
}
