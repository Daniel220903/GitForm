document.addEventListener('DOMContentLoaded', () => {
    const selectAllCheckbox = document.getElementById('select-all');
    const userCheckboxes = document.querySelectorAll('.user-checkbox');
    const btnBorrar = document.getElementById('btn-delete');
    const btnBloquear = document.getElementById('btn-ban');
    const btnDesbloquear = document.getElementById('btn-unban');
    const btnAddAdmin = document.getElementById('btn-add-admin');
    const btnDeleteAdmin = document.getElementById('btn-delete-admin');
    const form = document.getElementById('user-actions-form');
    const selectedUserIdsInput = document.getElementById('selected-user-ids');
    const actionTypeInput = document.getElementById('action-type');

    function updateButtons() {
        const anyChecked = Array.from(userCheckboxes).some(cb => cb.checked);
        btnBorrar.disabled = !anyChecked;
        btnBloquear.disabled = !anyChecked
        btnDesbloquear.disabled = !anyChecked;
        btnAddAdmin.disabled = !anyChecked;
        btnDeleteAdmin.disabled = !anyChecked;
    }

    selectAllCheckbox.addEventListener('change', function () {
        userCheckboxes.forEach(checkbox => {
            checkbox.checked = this.checked;
        });
        updateButtons();
    });

    userCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', updateButtons);
    });

    function submitForm(actionType) {
        const selectedUsers = Array.from(userCheckboxes)
            .filter(cb => cb.checked)
            .map(cb => cb.getAttribute('data-user-id'));

        const payload = {
            actionType: actionType,
            selectedUsers: selectedUsers
        };

        fetch('/Admin/AdminAction', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        }).then(response => {
            if (response.ok) {
                window.location.href = '/Admin/Index';
            } else {
                console.error('Error en la solicitud:', response.statusText);
            }
        })
            .catch(error => console.error('Error de red:', error));
    }

    btnBorrar.addEventListener('click', () => submitForm('borrar'));
    btnBloquear.addEventListener('click', () => submitForm('bloquear'));
    btnDesbloquear.addEventListener('click', () => submitForm('desbloquear'));
    btnAddAdmin.addEventListener('click', () => submitForm('addAdmin'));
    btnDeleteAdmin.addEventListener('click', () => submitForm('deleteAdmin'));
});