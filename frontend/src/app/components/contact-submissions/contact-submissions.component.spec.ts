import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ContactSubmissionsComponent } from './contact-submissions.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('ContactSubmissionsComponent', () => {
  let component: ContactSubmissionsComponent;
  let fixture: ComponentFixture<ContactSubmissionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContactSubmissionsComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ContactSubmissionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
