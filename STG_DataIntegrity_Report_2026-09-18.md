# Trainify — Reporte de integridad de datos STG

Fecha de ejecución: 18 de septiembre de 2026  
Base analizada: STG  
Método: `RH_CM/DataIntegrityAudit.ReadOnly.sql`  
Seguridad: auditoría de solo lectura; no se corrigieron ni modificaron datos.

## Dictamen ejecutivo

La cadena nueva de registro de exámenes funcionó correctamente en las pruebas E2E y la auditoría actual no encontró movimientos internos sin sus respuestas correspondientes. También se confirmó que los empleados activos no están asociados a departamentos o posiciones inactivos.

La base sí contiene inconsistencias históricas referenciales que deben analizarse antes de declarar los reportes históricos completamente confiables. No parecen haber sido generadas por la prueba ni por el flujo nuevo.

## Resumen

| Severidad | Validación | Hallazgos | Interpretación |
|---|---|---:|---|
| Crítica | Empleados sin departamento | 0 | Correcto |
| Crítica | Empleados sin posición | 0 | Correcto |
| Crítica | Asignaciones sin curso | 0 | Correcto |
| Crítica | Asignaciones sin posición | 12 | Referencias históricas rotas |
| Crítica | Movimientos sin asignación | 74 | El historial no puede resolver el curso/asignación |
| Crítica | Movimientos sin empleado | 3 | El historial no puede resolver al empleado |
| Crítica | Finalizaciones sin asignación | 315 | La finalización no puede resolver el curso/asignación |
| Crítica | Finalizaciones sin empleado | 116 | La finalización no puede resolver al empleado |
| Alta | Empleados activos en departamento inactivo | 0 | Correcto |
| Alta | Empleados activos en posición inactiva | 0 | Correcto |
| Alta | Movimientos internos de examen sin respuestas | 0 | Correcto; evidencia y movimiento están sincronizados |
| Alta | Códigos de examen usados por varios empleados | 78 | Dato histórico; el código nuevo ya evita reutilización y filtra por empleado |
| Revisión | Finalizaciones internas sin movimiento aprobado | 2,596 | Pueden ser cargas manuales/importadas; no deben clasificarse automáticamente como error |

## Lectura de los resultados

### Confirmaciones positivas

- El flujo interno de examen no presenta movimientos sin respuestas.
- No hay empleados activos enlazados a catálogos inactivos.
- No hay empleados apuntando a departamentos o posiciones inexistentes.
- No hay asignaciones que apunten a cursos inexistentes.
- Las pruebas temporales fueron eliminadas y no dejaron evidencia funcional residual.

### Inconsistencias referenciales reales

- Existen 12 asignaciones cuyo `FK_Position` ya no existe.
- Existen 77 movimientos únicos con al menos una referencia rota: 74 sin asignación y 3 sin empleado.
- Existen 415 finalizaciones únicas con al menos una referencia rota. Las sumas individuales son mayores porque 16 filas carecen tanto de asignación como de empleado.

Estas filas pueden producir cursos sin nombre, empleados vacíos, omisiones en reportes o resultados diferentes dependiendo del tipo de `JOIN` usado.

### Datos que requieren clasificación de negocio

Las 2,596 finalizaciones internas sin movimiento aprobado no deben eliminarse ni marcarse como error de forma automática. Es necesario clasificarlas por `CreateUser`, fecha y origen para determinar cuáles corresponden a:

- cargas iniciales o migraciones;
- cursos registrados manualmente por RH;
- importaciones masivas válidas;
- finalizaciones realmente inconsistentes.

### Códigos históricos de examen

Se encontraron 78 valores de `CODE_EXAM` compartidos por más de un empleado. El código de la aplicación fue reforzado para:

- generar cada código nuevo por encima del máximo existente y bajo un bloqueo transaccional;
- consultar y borrar evidencia usando `CODE_EXAM + número de control`;
- calcular resultados únicamente con las respuestas del empleado solicitado.

Esto evita nuevas colisiones y evita mezclar historiales existentes sin modificar la base de producción.

## Recomendación para producción

1. Ejecutar el script de solo lectura en producción y guardar los siete resultados.
2. Comparar los conteos contra STG; no asumir que las mismas filas existen en ambos ambientes.
3. Considerar bloqueantes únicamente las referencias rotas que afecten registros activos o reportes obligatorios.
4. Clasificar las finalizaciones manuales por origen antes de definir cualquier remediación.
5. No ejecutar correcciones SQL directas. Si se requiere remediar datos, implementar un flujo controlado y auditable desde la aplicación o una herramienta de migración aprobada.

