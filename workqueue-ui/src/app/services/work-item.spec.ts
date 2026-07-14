import { TestBed } from '@angular/core/testing';

import { WorkItem } from './work-item';

describe('WorkItem', () => {
  let service: WorkItem;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(WorkItem);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
