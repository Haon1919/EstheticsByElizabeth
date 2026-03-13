import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminAppointmentsComponent } from './admin-appointments.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('AdminAppointmentsComponent', () => {
  let component: AdminAppointmentsComponent;
  let fixture: ComponentFixture<AdminAppointmentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminAppointmentsComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AdminAppointmentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
