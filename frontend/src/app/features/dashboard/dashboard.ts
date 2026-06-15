import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PortalService } from '../../core/api/portal.service';
import { Dashboard as DashboardDto } from '../../core/api/models';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe],
  template: `
    <h1>Panel</h1>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (data(); as d) {
      <section class="cards">
        <article class="card">
          <span class="metric">{{ d.campanasActivas }}</span>
          <span class="label">Campañas activas</span>
        </article>
        <article class="card">
          <span class="metric">{{ d.ultimosInformesPublicados.length }}</span>
          <span class="label">Informes recientes</span>
        </article>
        <article class="card">
          <span class="metric">{{ d.kpis.length }}</span>
          <span class="label">Indicadores (KPIs)</span>
        </article>
      </section>

      @if (d.resumenAnalisis) {
        <section class="analisis">
          <h2>Análisis ambiental</h2>
          <p>{{ d.resumenAnalisis }}</p>
        </section>
      }

      <h2>Últimos informes publicados</h2>
      @if (d.ultimosInformesPublicados.length === 0) {
        <p class="empty">Aún no hay informes publicados.</p>
      } @else {
        <ul class="lista">
          @for (informe of d.ultimosInformesPublicados; track informe.id) {
            <li>
              <a [routerLink]="['/portal/informes', informe.id]">{{ informe.titulo }}</a>
              <span class="meta">{{ informe.tipoEstudio }} · {{ informe.creadoUtc | date: 'mediumDate' }}</span>
            </li>
          }
        </ul>
      }

      @if (d.kpis.length === 0) {
        <p class="nota">Los indicadores ambientales se mostrarán al integrar los datos de laboratorio (M4) e IA (M6).</p>
      }
    } @else {
      <p class="cargando">Cargando…</p>
    }
  `,
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly portal = inject(PortalService);
  readonly data = signal<DashboardDto | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    this.portal.dashboard().subscribe({
      next: (d) => this.data.set(d),
      error: () => this.error.set('No se pudo cargar el panel.'),
    });
  }
}
