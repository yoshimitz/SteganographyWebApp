$(document).ready(function () {
    $('#dataTable').dataTable({
        'columnDefs': [
            { 'orderData': [3], 'targets': [2] },
            {
                'targets': [3],
                'visible': false,
                'searchable': false
            }
        ]
    });
});