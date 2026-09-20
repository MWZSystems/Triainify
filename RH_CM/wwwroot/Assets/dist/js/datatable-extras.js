(function () {
    'use strict';

    if (typeof $ === 'undefined' || !$.fn || !$.fn.dataTable) {
        return;
    }

    // app.js (el bundle del theme) ya trae DataTables 1.10.10 + Responsive + una
    // versión de la extensión Buttons compatible con ese core (copy/csv/excel/print
    // funcionan). Lo que NO trae es ColVis (mostrar/ocultar columnas), así que eso se
    // implementa aquí como una segunda "feature" custom de DataTables, registrada bajo
    // la letra 'C' del dom string (Buttons ya usa 'B'). No se agrega ninguna librería
    // externa de Buttons: usar una versión más nueva rompe este core viejo porque
    // asume métodos que solo existen en DataTables 2.x (column().title(), util.stripHtml).

    window.dtExportButtons = function (options) {
        var opts = options || {};
        var exportOptions = { columns: 'th:not(.no-export)' };
        var buttons = [];

        if (opts.excel !== false) {
            buttons.push({ extend: 'excelHtml5', text: '<i class="fas fa-file-excel me-1"></i>Excel', exportOptions: exportOptions });
        }
        buttons.push({ extend: 'print', text: '<i class="fas fa-print me-1"></i>Print', exportOptions: exportOptions });

        return buttons;
    };

    // El dom string clásico de DataTables 1.10 no envuelve cada letra en filas/columnas
    // de Bootstrap por sí solo -- sin eso, "Show entries" y "Search" (l y f) quedan cada
    // uno en su propio div de bloque y se apilan en vez de quedar en la misma fila. Se
    // arma explícitamente como filas Bootstrap: Buttons arriba, luego length+search en
    // una fila, la tabla, y por último info+paginación en otra fila.
    window.dtButtonsDom =
        "B" +
        "<'row mb-2 align-items-center'<'col-6'l><'col-6'f>>" +
        "<'row'<'col-sm-12'tr>>" +
        "<'row mt-2 align-items-center'<'col-6'i><'col-6'p>>";

    function buildColvisGroup(api, $firstRow) {
        var $wrap = $('<div class="btn-group dt-colvis-group"></div>');
        var $btn = $('<button type="button" class="btn btn-dt-colvis btn-sm dropdown-toggle" data-bs-toggle="dropdown" data-bs-auto-close="outside" aria-expanded="false"><i class="fas fa-table-columns me-1"></i>Columns</button>');
        var $menu = $('<ul class="dropdown-menu"></ul>').on('click', function (e) { e.stopPropagation(); });

        api.columns().every(function (idx) {
            var col = this;
            var title = $firstRow.find('th').eq(idx).text().trim();
            if (!title) return;

            var $checkbox = $('<input type="checkbox" class="form-check-input me-2">').prop('checked', col.visible());
            $checkbox.on('change', function () {
                col.visible(!col.visible());
            });

            var $label = $('<label class="dropdown-item form-check"></label>').append($checkbox).append(document.createTextNode(title));
            $menu.append($('<li></li>').append($label));
        });

        return $wrap.append($btn).append($menu);
    }

    // Varias vistas tienen una 2a fila en <thead> (inputs de filtro por columna). Por
    // cómo DataTables arma aoColumns, column().header() termina apuntando a esa 2a
    // fila (vacía), no a la fila con los títulos reales. Eso rompe tanto el título que
    // usa el menú de columnas de abajo, como la exclusión "th.no-export" que usa el
    // botón Excel/Print (exportOptions) si internamente también resuelve por
    // column().header(). Se arregla una sola vez por tabla, copiando la clase
    // "no-export" de la fila con títulos a las demás filas de <thead>/<tfoot>.
    //
    // El botón "Columns" se agrega aquí también (en vez de como una "feature" de dom
    // separada) para que quede DENTRO del mismo contenedor .dt-buttons que Excel/Print
    // (que arma la extensión Buttons empaquetada en app.js) y los tres botones queden
    // en una sola fila, no en dos grupos que se acomodan por separado.
    if (!window.__dtButtonsExtrasBound) {
        window.__dtButtonsExtrasBound = true;
        $(document).on('init.dt', function (e, settings) {
            var api = new $.fn.dataTable.Api(settings);
            var $table = $(settings.nTable);
            var $headRows = $table.find('thead tr');

            if ($headRows.length > 1) {
                $headRows.eq(0).find('th.no-export').each(function () {
                    var idx = $(this).index();
                    $table.find('thead tr, tfoot tr').each(function (i, tr) {
                        if (i === 0) return;
                        $(tr).find('th').eq(idx).addClass('no-export');
                    });
                });
            }

            var $dtButtons = $(api.table().container()).find('div.dt-buttons').first();
            if ($dtButtons.length) {
                $dtButtons.append(buildColvisGroup(api, $headRows.first()));
            }
        });
    }
})();
