import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { provideZoneChangeDetection } from '@angular/core';

import { StoryListComponent } from './story-list.component';
import { HackerNewsService } from '../../services/hacker-news.service';
import { Story, PaginatedStories, StoryType } from '../../interfaces/story.interface';

describe('StoryListComponent', () => {
  let component: StoryListComponent;
  let fixture: ComponentFixture<StoryListComponent>;
  let mockHackerNewsService: jasmine.SpyObj<HackerNewsService>;

  const mockStories: Story[] = [
    {
      id: 1,
      title: 'Test Story 1',
      url: 'https://example.com/1',
      by: 'user1',
      time: 1640995200,
      score: 100,
      descendants: 25,
      type: 'story' as StoryType
    },
    {
      id: 2,
      title: 'Test Story 2',
      url: 'https://example.com/2',
      by: 'user2',
      time: 1640995100,
      score: 85,
      descendants: 12,
      type: 'story' as StoryType
    }
  ];

  const mockPaginatedStories: PaginatedStories = {
    stories: mockStories,
    totalCount: 100,
    currentPage: 1,
    totalPages: 5,
    pageSize: 20
  };

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('HackerNewsService', [
      'getNewStories',
      'searchStories',
      'formatTimeAgo',
      'hasValidUrl',
      'getStoryUrl'
    ]);

    await TestBed.configureTestingModule({
      imports: [StoryListComponent, HttpClientTestingModule],
      providers: [
        { provide: HackerNewsService, useValue: spy },
        provideZoneChangeDetection({ eventCoalescing: true })
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StoryListComponent);
    component = fixture.componentInstance;
    mockHackerNewsService = TestBed.inject(HackerNewsService) as jasmine.SpyObj<HackerNewsService>;
    
    // Default mock implementations
    mockHackerNewsService.getNewStories.and.returnValue(of(mockPaginatedStories));
    mockHackerNewsService.searchStories.and.returnValue(of(mockStories));
    mockHackerNewsService.formatTimeAgo.and.returnValue('2 hours ago');
    mockHackerNewsService.hasValidUrl.and.returnValue(true);
    mockHackerNewsService.getStoryUrl.and.returnValue('https://example.com/1');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Component Initialization', () => {
    it('should load stories on init', () => {
      component.ngOnInit();
      expect(mockHackerNewsService.getNewStories).toHaveBeenCalledWith(1, 20);
      expect(component.stories()).toEqual(mockStories);
      expect(component.currentPage()).toBe(1);
      expect(component.totalPages()).toBe(5);
      expect(component.totalCount()).toBe(100);
    });


  });

  describe('Error Handling', () => {
    it('should handle service errors', () => {
      const errorMessage = 'Service error';
      mockHackerNewsService.getNewStories.and.returnValue(throwError(() => new Error(errorMessage)));
      
      component.loadStories();
      
      expect(component.error()).toBe('Failed to load stories. Please try again.');
      expect(component.loading()).toBe(false);
    });
  });

  describe('Search Functionality', () => {
    beforeEach(() => {
      component.stories.set(mockStories);
    });



    it('should show all stories when search is empty', () => {
      component.stories.set(mockStories);
      component.searchQuery.set('');
      component.filterStories();
      
      expect(component.filteredStories()).toEqual(mockStories);
    });
  });

  describe('Pagination', () => {
    beforeEach(() => {
      component.currentPage.set(2);
      component.totalPages.set(5);
    });

    it('should go to next page', () => {
      spyOn(component, 'loadStories');
      component.nextPage();
      expect(component.loadStories).toHaveBeenCalledWith(3);
    });

    it('should go to previous page', () => {
      spyOn(component, 'loadStories');
      component.previousPage();
      expect(component.loadStories).toHaveBeenCalledWith(1);
    });

    it('should not go beyond last page', () => {
      component.currentPage.set(5);
      spyOn(component, 'loadStories');
      component.nextPage();
      expect(component.loadStories).not.toHaveBeenCalled();
    });

    it('should not go below first page', () => {
      component.currentPage.set(1);
      spyOn(component, 'loadStories');
      component.previousPage();
      expect(component.loadStories).not.toHaveBeenCalled();
    });

    it('should generate correct page numbers', () => {
      component.currentPage.set(3);
      component.totalPages.set(10);
      
      const pageNumbers = component.getPageNumbers();
      expect(pageNumbers).toContain(1);
      expect(pageNumbers).toContain(3);
      expect(pageNumbers).toContain(10);
    });
  });

  describe('Story URL Handling', () => {
    it('should detect valid URLs', () => {
      const storyWithUrl = mockStories[0];
      mockHackerNewsService.hasValidUrl.and.returnValue(true);
      expect(component.hasValidUrl(storyWithUrl)).toBe(true);
    });

    it('should detect invalid URLs', () => {
      const storyWithoutUrl = { ...mockStories[0], url: undefined };
      mockHackerNewsService.hasValidUrl.and.returnValue(false);
      expect(component.hasValidUrl(storyWithoutUrl)).toBe(false);
    });

    it('should return story URL for valid URLs', () => {
      const story = mockStories[0];
      mockHackerNewsService.getStoryUrl.and.returnValue('https://example.com/1');
      expect(component.getStoryUrl(story)).toBe('https://example.com/1');
    });

    it('should return HN discussion URL for invalid URLs', () => {
      const story = { ...mockStories[0], url: undefined };
      mockHackerNewsService.getStoryUrl.and.returnValue('https://news.ycombinator.com/item?id=1');
      expect(component.getStoryUrl(story)).toBe('https://news.ycombinator.com/item?id=1');
    });
  });

  describe('Template Rendering', () => {
    beforeEach(() => {
      component.stories.set(mockStories);
      component.filteredStories.set(mockStories);
      component.loading.set(false);
      component.error.set(null);
      fixture.detectChanges();
    });

    it('should render story list', () => {
      const storyElements = fixture.debugElement.queryAll(By.css('.story-item'));
      expect(storyElements.length).toBe(2);
    });

    it('should render story titles', () => {
      const titleElements = fixture.debugElement.queryAll(By.css('.story-title a'));
      expect(titleElements[0].nativeElement.textContent.trim()).toContain('Test Story 1');
      expect(titleElements[1].nativeElement.textContent.trim()).toContain('Test Story 2');
    });

    it('should render story metadata', () => {
      const metaElements = fixture.debugElement.queryAll(By.css('.story-meta'));
      expect(metaElements.length).toBe(2);
    });

    it('should show loading spinner when loading', () => {
      component.loading.set(true);
      fixture.detectChanges();
      
      const loadingElement = fixture.debugElement.query(By.css('.loading-container'));
      expect(loadingElement).toBeTruthy();
    });

    it('should show error message when error occurs', () => {
      component.error.set('Test error message');
      fixture.detectChanges();
      
      const errorElement = fixture.debugElement.query(By.css('.error-container'));
      expect(errorElement).toBeTruthy();
      expect(errorElement.nativeElement.textContent).toContain('Test error message');
    });

    it('should show no results message when search returns empty', () => {
      component.filteredStories.set([]);
      component.searchQuery.set('nonexistent');
      fixture.detectChanges();
      
      const noResultsElement = fixture.debugElement.query(By.css('.no-results'));
      expect(noResultsElement).toBeTruthy();
    });
  });

  describe('Component Methods', () => {
    it('should format time correctly', () => {
      const timestamp = 1640995200;
      mockHackerNewsService.formatTimeAgo.and.returnValue('2 hours ago');
      
      const result = component.getTimeAgo(timestamp);
      expect(result).toBe('2 hours ago');
      expect(mockHackerNewsService.formatTimeAgo).toHaveBeenCalledWith(timestamp);
    });
  });
});
