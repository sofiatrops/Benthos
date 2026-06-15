import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AdminService, TIPOS_CAMPANA } from '../../core/api/admin.service';
import { Campania, Centro, Empresa } from '../../core/api/models';

@Component({
  selector: 'app-empresa-detail',
  imports: [RouterLink, DatePipe, ReactiveFormsModule],
  template: `
    <a routerLink="/admin/empresas" class="volver">← Volver a empresas</a>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (empresa(); as e) {
      <h1>{{ e.razonSocial }}</h1>
      <dl class="meta-grid">
        <div><dt>RUT</dt><dd>{{ e.rut }}</dd></div>
        <div><dt>Rubro</dt><dd>{{ e.rubro }}</dd></div>
        <div><dt>Estado</dt><dd>{{ e.activa ? 'Activa' : 'Inactiva' }}</dd></div>
        <div><dt>Registrada</dt><dd>{{ e.creadaUtc | date: 'mediumDate' }}</dd></div>
      </dl>

      <h2>Centros</h2>
      @if (centros().length === 0) {
        <p class="empty">Sin centros.</p>
      } @else {
        <table class="tabla">
          <thead><tr><th>Nombre</th><th>Código</th><th>Región</th><th>Coordenadas</th></tr></thead>
          <tbody>
            @for (c of centros(); track c.id) {
              <tr>
                <td>{{ c.nombre }}</td><td>{{ c.codigoInterno }}</td><td>{{ c.region }}</td>
                <td>{{ c.latitud }}, {{ c.longitud }}</td>
              </tr>
            }
          </tbody>
        </table>
      }

      <details class="form-card" [open]="centroForm.dirty">
        <summary>Registrar centro</summary>
        <form [formGroup]="centroForm" (ngSubmit)="crearCentro()">
          <label>Nombre <input formControlName="nombre" type="text" /></label>
          <label>Código interno <input formControlName="codigoInterno" type="text" /></label>
          <label>Región <input formControlName="region" type="text" /></label>
          <label>Latitud <input formControlName="latitud" type="number" step="any" /></label>
          <label>Longitud <input formControlName="longitud" type="number" step="any" /></label>
          <div class="acciones">
            <button type="submit" class="primary" [disabled]="centroForm.invalid || enviando()">Registrar centro</button>
            @if (centroError()) { <span class="error">{{ centroError() }}</span> }
          </div>
        </form>
      </details>

      <h2>Campañas</h2>
      @if (campanas().length === 0) {
        <p class="empty">Sin campañas.</p>
      } @else {
        <table class="tabla">
          <thead><tr><th>Nombre</th><th>Tipo</th><th>Estado</th><th>Período</th></tr></thead>
          <tbody>
            @for (c of campanas(); track c.id) {
              <tr>
                <td>{{ c.nombre }}</td><td>{{ c.tipo }}</td><td>{{ c.estado }}</td>
                <td>{{ c.fechaInicio | date: 'mediumDate' }} – {{ c.fechaFin | date: 'mediumDate' }}</td>
              </tr>
            }
          </tbody>
        </table>
      }

      <details class="form-card" [open]="campanaForm.dirty">
        <summary>Crear campaña</summary>
        <form [formGroup]="campanaForm" (ngSubmit)="crearCampana()">
          <label>Nombre <input formControlName="nombre" type="text" /></label>
          <label>Descripción <input formControlName="descripcion" type="text" /></label>
          <label>Tipo
            <select formControlName="tipo">
              @for (t of tipos; track t) { <option [value]="t">{{ t }}</option> }
            </select>
          </label>
          <label>Fecha inicio <input formControlName="fechaInicio" type="date" /></label>
          <label>Fecha fin <input formControlName="fechaFin" type="date" /></label>
          <label>Centros (selección múltiple)
            <select formControlName="centroIds" multiple size="3">
              @for (c of centros(); track c.id) { <option [value]="c.id">{{ c.nombre }}</option> }
            </select>
          </label>
          <div class="acciones">
            <button type="submit" class="primary"
              [disabled]="campanaForm.invalid || centros().length === 0 || enviando()">Crear campaña</button>
            @if (centros().length === 0) { <span class="empty">Registra un centro primero.</span> }
            @if (campanaError()) { <span class="error">{{ campanaError() }}</span> }
          </div>
        </form>
      </details>
    } @else {
      <p class="cargando">Cargando…</p>
    }
  `,
  styleUrl: './admin.scss',
})
export class EmpresaDetail implements OnInit {
  readonly id = input.required<string>();

  private readonly admin = inject(AdminService);
  private readonly fb = inject(FormBuilder);

  readonly empresa = signal<Empresa | null>(null);
  readonly centros = signal<Centro[]>([]);
  readonly campanas = signal<Campania[]>([]);
  readonly error = signal<string | null>(null);
  readonly centroError = signal<string | null>(null);
  readonly campanaError = signal<string | null>(null);
  readonly enviando = signal(false);

  protected readonly tipos = TIPOS_CAMPANA;

  readonly centroForm = this.fb.nonNullable.group({
    nombre: ['', Validators.required],
    codigoInterno: ['', Validators.required],
    region: [''],
    latitud: [0, [Validators.required, Validators.min(-90), Validators.max(90)]],
    longitud: [0, [Validators.required, Validators.min(-180), Validators.max(180)]],
  });

  readonly campanaForm = this.fb.nonNullable.group({
    nombre: ['', Validators.required],
    descripcion: [''],
    tipo: [TIPOS_CAMPANA[0] as string, Validators.required],
    fechaInicio: ['', Validators.required],
    fechaFin: ['', Validators.required],
    centroIds: [[] as string[], Validators.required],
  });

  ngOnInit(): void {
    this.cargar();
  }

  protected crearCentro(): void {
    if (this.centroForm.invalid) {
      return;
    }
    this.enviando.set(true);
    this.centroError.set(null);
    this.admin.crearCentro(this.id(), this.centroForm.getRawValue()).subscribe({
      next: () => {
        this.centroForm.reset({ latitud: 0, longitud: 0 });
        this.enviando.set(false);
        this.recargarCentros();
      },
      error: (e) => {
        this.enviando.set(false);
        this.centroError.set(this.mensaje(e, 'No se pudo registrar el centro.'));
      },
    });
  }

  protected crearCampana(): void {
    if (this.campanaForm.invalid) {
      return;
    }
    this.enviando.set(true);
    this.campanaError.set(null);
    this.admin.crearCampana(this.id(), this.campanaForm.getRawValue()).subscribe({
      next: () => {
        this.campanaForm.reset({ tipo: TIPOS_CAMPANA[0], centroIds: [] });
        this.enviando.set(false);
        this.recargarCampanas();
      },
      error: (e) => {
        this.enviando.set(false);
        this.campanaError.set(this.mensaje(e, 'No se pudo crear la campaña.'));
      },
    });
  }

  private cargar(): void {
    const id = this.id();
    forkJoin({
      empresa: this.admin.empresa(id),
      centros: this.admin.centros(id),
      campanas: this.admin.campanas(id),
    }).subscribe({
      next: (r) => {
        this.empresa.set(r.empresa);
        this.centros.set(r.centros.items);
        this.campanas.set(r.campanas.items);
      },
      error: () => this.error.set('No se pudo cargar la empresa.'),
    });
  }

  private recargarCentros(): void {
    this.admin.centros(this.id()).subscribe((r) => this.centros.set(r.items));
  }

  private recargarCampanas(): void {
    this.admin.campanas(this.id()).subscribe((r) => this.campanas.set(r.items));
  }

  private mensaje(e: unknown, fallback: string): string {
    return (e as { error?: { detail?: string } })?.error?.detail ?? fallback;
  }
}
