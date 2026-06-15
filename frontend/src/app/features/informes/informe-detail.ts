import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PortalService } from '../../core/api/portal.service';
import { InformePublicadoDetalle } from '../../core/api/models';

@Component({
  selector: 'app-informe-detail',
  imports: [RouterLink, DatePipe],
  template: `
    <a routerLink="/portal/informes" class="volver">← Volver a informes</a>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (informe(); as i) {
      <h1>{{ i.titulo }}</h1>
      <dl class="meta">
        <div><dt>Tipo de estudio</dt><dd>{{ i.tipoEstudio }}</dd></div>
        <div><dt>Período</dt><dd>{{ i.periodoDesde | date: 'mediumDate' }} – {{ i.periodoHasta | date: 'mediumDate' }}</dd></div>
        <div><dt>Versión vigente</dt><dd>v{{ i.versionVigenteNumero }}</dd></div>
        @if (i.fechaAprobacionUtc) {
          <div><dt>Aprobado</dt><dd>{{ i.fechaAprobacionUtc | date: 'medium' }}</dd></div>
        }
      </dl>

      @if (i.urlDescarga) {
        <a class="descarga primary" [href]="i.urlDescarga" target="_blank" rel="noopener">
          Descargar informe (PDF)
        </a>
      } @else {
        <p class="empty">La versión vigente no tiene archivo disponible.</p>
      }

      @if (i.anexos.length > 0) {
        <h2>Anexos</h2>
        <ul class="anexos">
          @for (anexo of i.anexos; track anexo.urlDescarga) {
            <li>
              <a [href]="anexo.urlDescarga" target="_blank" rel="noopener">{{ anexo.descripcion }}</a>
              <span class="fecha">{{ anexo.fechaUtc | date: 'mediumDate' }}</span>
            </li>
          }
        </ul>
      }
    } @else {
      <p class="cargando">Cargando…</p>
    }
  `,
  styleUrl: './informes.scss',
})
export class InformeDetail implements OnInit {
  /** Parámetro de ruta `:id` (binding de entrada de ruta, Angular 20). */
  readonly id = input.required<string>();

  private readonly portal = inject(PortalService);
  readonly informe = signal<InformePublicadoDetalle | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.portal.informe(this.id()).subscribe({
      next: (i) => this.informe.set(i),
      error: () => this.error.set('No se pudo cargar el informe o no está disponible.'),
    });
  }
}
