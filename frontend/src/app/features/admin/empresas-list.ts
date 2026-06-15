import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../core/api/admin.service';
import { Empresa, PagedResult } from '../../core/api/models';

@Component({
  selector: 'app-empresas-list',
  imports: [RouterLink, ReactiveFormsModule],
  template: `
    <h1>Empresas</h1>

    <details class="form-card" [open]="form.dirty">
      <summary>Registrar empresa</summary>
      <form [formGroup]="form" (ngSubmit)="registrar()">
        <label>Razón social
          <input formControlName="razonSocial" type="text" />
        </label>
        <label>RUT (con dígito verificador, p. ej. 76000001-9)
          <input formControlName="rut" type="text" />
        </label>
        <label>Rubro
          <input formControlName="rubro" type="text" />
        </label>
        <div class="acciones">
          <button type="submit" class="primary" [disabled]="form.invalid || enviando()">Registrar</button>
          @if (formError()) { <span class="error">{{ formError() }}</span> }
        </div>
      </form>
    </details>

    @if (error()) {
      <p class="error">{{ error() }}</p>
    } @else if (pagina(); as p) {
      @if (p.items.length === 0) {
        <p class="empty">No hay empresas registradas.</p>
      } @else {
        <table class="tabla">
          <thead>
            <tr><th>Razón social</th><th>RUT</th><th>Rubro</th><th>Estado</th></tr>
          </thead>
          <tbody>
            @for (empresa of p.items; track empresa.id) {
              <tr>
                <td><a [routerLink]="['/admin/empresas', empresa.id]">{{ empresa.razonSocial }}</a></td>
                <td>{{ empresa.rut }}</td>
                <td>{{ empresa.rubro }}</td>
                <td>{{ empresa.activa ? 'Activa' : 'Inactiva' }}</td>
              </tr>
            }
          </tbody>
        </table>
        <p class="meta">{{ p.totalCount }} empresa(s)</p>
      }
    } @else {
      <p class="cargando">Cargando…</p>
    }
  `,
  styleUrl: './admin.scss',
})
export class EmpresasList {
  private readonly admin = inject(AdminService);
  private readonly fb = inject(FormBuilder);

  readonly pagina = signal<PagedResult<Empresa> | null>(null);
  readonly error = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly enviando = signal(false);

  readonly form = this.fb.nonNullable.group({
    razonSocial: ['', [Validators.required, Validators.maxLength(300)]],
    rut: ['', Validators.required],
    rubro: [''],
  });

  constructor() {
    this.cargar();
  }

  protected registrar(): void {
    if (this.form.invalid) {
      return;
    }
    this.enviando.set(true);
    this.formError.set(null);
    this.admin.crearEmpresa(this.form.getRawValue()).subscribe({
      next: () => {
        this.form.reset();
        this.enviando.set(false);
        this.cargar();
      },
      error: (e) => {
        this.enviando.set(false);
        this.formError.set(this.mensaje(e));
      },
    });
  }

  private cargar(): void {
    this.admin.empresas().subscribe({
      next: (p) => this.pagina.set(p),
      error: () => this.error.set('No se pudieron cargar las empresas.'),
    });
  }

  private mensaje(e: unknown): string {
    const detail = (e as { error?: { detail?: string } })?.error?.detail;
    return detail ?? 'No se pudo registrar la empresa.';
  }
}
