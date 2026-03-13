import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClientManagementComponent } from './client-management.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('ClientManagementComponent', () => {
  let component: ClientManagementComponent;
  let fixture: ComponentFixture<ClientManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClientManagementComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ClientManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
