import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PortalService } from '../../core/api/portal.service';
import { InformeResumen, PagedResult } from '../../core/api/models';

@Component({
  selector: 'app-informes-list',
  imports: [RouterLink, DatePipe],
  template: `
    <h1>Informes publicados</h1>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (pagina(); as p) {
      @if (p.items.length === 0) {
        <p class="empty">No hay informes publicados para su organización.</p>
      } @else {
        <table class="tabla">
          <thead>
            <tr>
              <th>Título</th>
              <th>Tipo de estudio</th>
              <th>Período</th>
              <th>Versión</th>
            </tr>
          </thead>
          <tbody>
            @for (informe of p.items; track informe.id) {
              <tr>
                <td><a [routerLink]="['/informes', informe.id]">{{ informe.titulo }}</a></td>
                <td>{{ informe.tipoEstudio }}</td>
                <td>{{ informe.periodoDesde | date: 'mediumDate' }} – {{ informe.periodoHasta | date: 'mediumDate' }}</td>
                <td>v{{ informe.versionVigenteNumero }}</td>
              </tr>
            }
          </tbody>
        </table>

        <p class="paginacion">
          {{ p.totalCount }} informe(s) · página {{ p.page }}
        </p>
      }
    } @else {
      <p class="cargando">Cargando…</p>
    }
  `,
  styleUrl: './informes.scss',
})
export class InformesList {
  private readonly portal = inject(PortalService);
  readonly pagina = signal<PagedResult<InformeResumen> | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    this.portal.informesPublicados().subscribe({
      next: (p) => this.pagina.set(p),
      error: () => this.error.set('No se pudieron cargar los informes.'),
    });
  }
}
