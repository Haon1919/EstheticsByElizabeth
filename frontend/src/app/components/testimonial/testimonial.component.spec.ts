import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TestimonialComponent } from './testimonial.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('TestimonialComponent', () => {
  let component: TestimonialComponent;
  let fixture: ComponentFixture<TestimonialComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestimonialComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(TestimonialComponent);
    component = fixture.componentInstance;
    component.testimonial = { id: 1, name: 'Test', quote: 'Test', rating: 5 };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
