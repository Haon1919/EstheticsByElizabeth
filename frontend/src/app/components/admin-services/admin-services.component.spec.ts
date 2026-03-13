import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminServicesComponent } from './admin-services.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('AdminServicesComponent', () => {
  let component: AdminServicesComponent;
  let fixture: ComponentFixture<AdminServicesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminServicesComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AdminServicesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
