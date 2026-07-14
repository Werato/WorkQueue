import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WorkItems } from './work-items';

describe('WorkItems', () => {
  let component: WorkItems;
  let fixture: ComponentFixture<WorkItems>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkItems],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkItems);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
