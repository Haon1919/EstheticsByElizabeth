import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AftercareComponent } from './aftercare.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('AftercareComponent', () => {
  let component: AftercareComponent;
  let fixture: ComponentFixture<AftercareComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AftercareComponent, HttpClientTestingModule, RouterTestingModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AftercareComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
