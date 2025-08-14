import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { provideZoneChangeDetection } from '@angular/core';
import { HackerNewsService } from './hacker-news.service';
import { Story, PaginatedStories } from '../interfaces/story.interface';

describe('HackerNewsService', () => {
  let service: HackerNewsService;
  let httpMock: HttpTestingController;

  const mockStoryIds = [1, 2, 3, 4, 5];
  const mockStory: Story = {
    id: 1,
    title: 'Test Story',
    url: 'https://example.com',
    by: 'testuser',
    time: 1640995200,
    score: 100,
    descendants: 25,
    type: 'story'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        HackerNewsService,
        provideZoneChangeDetection({ eventCoalescing: true })
      ]
    });
    service = TestBed.inject(HackerNewsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getNewStories', () => {
    it('should fetch paginated stories from backend API', (done) => {
      const page = 1;
      const pageSize = 20;
      const mockResponse: PaginatedStories = {
        stories: [mockStory],
        totalCount: 100,
        currentPage: 1,
        totalPages: 5,
        pageSize: 20
      };

      service.getNewStories(page, pageSize).subscribe((result: PaginatedStories) => {
        expect(result.stories).toHaveSize(1);
        expect(result.currentPage).toBe(1);
        expect(result.totalCount).toBe(100);
        expect(result.totalPages).toBe(5);
        done();
      });

      const req = httpMock.expectOne('http://localhost:5248/api/stories/newest?page=1&pageSize=20');
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle empty story list', (done) => {
      const mockResponse: PaginatedStories = {
        stories: [],
        totalCount: 0,
        currentPage: 1,
        totalPages: 0,
        pageSize: 20
      };

      service.getNewStories(1, 20).subscribe((result: PaginatedStories) => {
        expect(result.stories).toHaveSize(0);
        expect(result.totalCount).toBe(0);
        expect(result.totalPages).toBe(0);
        done();
      });

      const req = httpMock.expectOne('http://localhost:5248/api/stories/newest?page=1&pageSize=20');
      req.flush(mockResponse);
    });
  });

  describe('getStoryById', () => {
    it('should fetch a single story from backend API', () => {
      service.getStoryById(1).subscribe((story: Story) => {
        expect(story).toEqual(mockStory);
      });

      const req = httpMock.expectOne('http://localhost:5248/api/stories/1');
      expect(req.request.method).toBe('GET');
      req.flush(mockStory);
    });
  });

  describe('searchStories', () => {
    it('should search stories through backend API', (done) => {
      const searchQuery = 'angular';
      const mockResults = [{ ...mockStory, title: 'Angular Tutorial' }];

      service.searchStories(searchQuery, 10).subscribe((stories) => {
        expect(stories).toHaveSize(1);
        expect(stories[0].title).toContain('Angular');
        done();
      });

      const req = httpMock.expectOne('http://localhost:5248/api/stories/search?q=angular&limit=10');
      expect(req.request.method).toBe('GET');
      req.flush(mockResults);
    });

    it('should return newest stories for empty query', (done) => {
      const mockResponse: PaginatedStories = {
        stories: [mockStory],
        totalCount: 100,
        currentPage: 1,
        totalPages: 5,
        pageSize: 50
      };

      service.searchStories('', 50).subscribe((stories) => {
        expect(stories).toHaveSize(1);
        done();
      });

      const req = httpMock.expectOne('http://localhost:5248/api/stories/newest?page=1&pageSize=50');
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('formatTimeAgo', () => {
    const mockNow = 1640995200000; // Fixed timestamp for testing

    beforeEach(() => {
      spyOn(Date, 'now').and.returnValue(mockNow);
    });

    it('should format seconds ago', () => {
      const timestamp = Math.floor((mockNow - 30000) / 1000); // 30 seconds ago
      const result = service.formatTimeAgo(timestamp);
      expect(result).toBe('Just now');
    });

    it('should format minutes ago', () => {
      const timestamp = Math.floor((mockNow - 300000) / 1000); // 5 minutes ago
      const result = service.formatTimeAgo(timestamp);
      expect(result).toBe('5 minutes ago');
    });

    it('should format hours ago', () => {
      const timestamp = Math.floor((mockNow - 7200000) / 1000); // 2 hours ago
      const result = service.formatTimeAgo(timestamp);
      expect(result).toBe('2 hours ago');
    });

    it('should format days ago', () => {
      const timestamp = Math.floor((mockNow - 172800000) / 1000); // 2 days ago
      const result = service.formatTimeAgo(timestamp);
      expect(result).toBe('2 days ago');
    });

    it('should handle singular units', () => {
      const timestamp1 = Math.floor((mockNow - 60000) / 1000); // 1 minute ago
      const timestamp2 = Math.floor((mockNow - 3600000) / 1000); // 1 hour ago
      const timestamp3 = Math.floor((mockNow - 86400000) / 1000); // 1 day ago

      expect(service.formatTimeAgo(timestamp1)).toBe('1 minute ago');
      expect(service.formatTimeAgo(timestamp2)).toBe('1 hour ago');
      expect(service.formatTimeAgo(timestamp3)).toBe('1 day ago');
    });
  });
});