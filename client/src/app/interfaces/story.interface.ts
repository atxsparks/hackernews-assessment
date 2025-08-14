export interface Story {
  id: number;
  title: string;
  url?: string;
  by: string;
  time: number;
  score: number;
  descendants?: number;
  type: StoryType;
}

export type StoryType = 'story' | 'job' | 'ask' | 'show' | 'poll';

export interface PaginatedStories {
  stories: Story[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
}

export interface TimeAgoResult {
  value: number;
  unit: 'day' | 'hour' | 'minute' | 'now';
}
