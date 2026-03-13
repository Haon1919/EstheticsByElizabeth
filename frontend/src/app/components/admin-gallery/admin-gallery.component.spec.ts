import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminGalleryComponent } from './admin-gallery.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('AdminGalleryComponent', () => {
  let component: AdminGalleryComponent;
  let fixture: ComponentFixture<AdminGalleryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminGalleryComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AdminGalleryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
