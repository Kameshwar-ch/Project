import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar';
import { TopbarComponent } from '../topbar/topbar';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent],
  template: `
    <div class="sf-app">
      <!-- Desktop Sidebar -->
      <app-sidebar class="d-none d-lg-block"></app-sidebar>

      <!-- Mobile Offcanvas Sidebar -->
      <div class="offcanvas offcanvas-start sf-offcanvas" tabindex="-1" id="mobileSidebar">
        <div class="offcanvas-header border-bottom" style="border-color:var(--sf-border)!important">
          <h5 class="offcanvas-title fw-bold">SkillForge</h5>
          <button type="button" class="btn-close" data-bs-dismiss="offcanvas"></button>
        </div>
        <div class="offcanvas-body p-0">
          <app-sidebar style="position:static;height:auto;border:none;width:100%;"></app-sidebar>
        </div>
      </div>

      <!-- Main area -->
      <div class="sf-main-area">
        <app-topbar></app-topbar>
        <main class="sf-content fade-in">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `
})
export class MainLayoutComponent {}
