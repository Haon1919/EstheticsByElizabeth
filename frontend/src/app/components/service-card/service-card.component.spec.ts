import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ServiceCardComponent } from './service-card.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('ServiceCardComponent', () => {
  let component: ServiceCardComponent;
  let fixture: ComponentFixture<ServiceCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServiceCardComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ServiceCardComponent);
    component = fixture.componentInstance;
    component.service = { id: 1, name: 'Test', description: 'Test', price: 10, categoryId: 1, image: 'test.jpg' };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
