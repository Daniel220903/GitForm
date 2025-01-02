﻿document.addEventListener('DOMContentLoaded', () => {

    const deleteButtons = document.querySelectorAll('.delete-template-button');

    // Asignar un evento de clic a cada uno
    deleteButtons.forEach(button => {
        button.addEventListener('click', function () {
            // Obtener el ID del template desde el atributo 'data-template-id'
            const templateId = this.getAttribute('data-template-id');
            showConfirmationModal(templateId);
        });
    });

    function showConfirmationModal(templateId) {
        const confirmButton = document.getElementById('confirmDeleteButton');

        // Verifica si el botón de confirmación existe
        if (confirmButton) {
            confirmButton.onclick = function () {
                sendDeleteRequest(templateId);
            };

            const modal = new bootstrap.Modal(document.getElementById('confirmationModal'));
            modal.show();
        } else {
            console.error('No se encontró el botón de confirmación');
        }
    }

    function sendDeleteRequest(templateId) {
        currentPage = 1;
        event.preventDefault();
        var searchTerm = "";

        $.ajax({
            url: '/api/Template/DeleteTemplate/' + templateId,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ searchTerm: searchTerm }),
            success: function (response) {
                console.log("rispouns", response);
                location.reload();

            },
            error: function (xhr, status, error) { }
        });
    }

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