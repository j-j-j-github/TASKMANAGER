// wwwroot/js/taskManager.js

$(document).ready(function () {
    loadTasks();
    $('#searchInput').on('input', function() {
        loadTasks(); // Reload table every time you type
    });
});

function loadTasks() {
    // 1. Get the current text from the search box
    let searchTerm = $('#searchInput').val();

    // 2. Send it to the server
    $.get('/Tasks/GetTasks?term=' + searchTerm, function (data) {
        let html = '';
        if (data.length === 0) {
            html = '<tr><td colspan="6" class="text-center">No tasks found.</td></tr>';
        } else {
            data.forEach(t => {
                // Color code the priority
                let badgeColor = t.priority === 'High' ? 'bg-danger' : 
                                 t.priority === 'Medium' ? 'bg-warning text-dark' : 'bg-success';

                html += `
                    <tr>
                        <td><strong>${t.title}</strong></td>
                        <td>${t.assignedToName}</td>
                        <td><span class="badge ${badgeColor}">${t.priority}</span></td>
                        <td>${t.deadline}</td>
                        <td>${t.status}</td>
                        <td>
                            <button class="btn btn-sm btn-info" onclick='editTask(${JSON.stringify(t)})'>Edit</button>
                            <button class="btn btn-sm btn-danger" onclick="deleteTask(${t.taskID})">Delete</button>
                        </td>
                    </tr>`;
            });
        }
        $('#taskTableBody').html(html);
    });
}

function openModal() {
    // Reset form for a New Task
    $('#taskId').val(0);
    $('#taskTitle').val('');
    $('#taskDeadline').val('');
    $('#taskPriority').val('Medium');
    $('#taskStatus').val('Pending');
    $('#taskAssignedTo').val(0); // Default to Unassigned
    
    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function editTask(task) {
    // Fill the form with existing data
    $('#taskId').val(task.taskID);
    $('#taskTitle').val(task.title);
    $('#taskPriority').val(task.priority);
    $('#taskDeadline').val(task.deadline);
    $('#taskStatus').val(task.status);
    
    // Select the correct user in the dropdown (or 0 if null)
    $('#taskAssignedTo').val(task.assignedTo || 0);

    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function saveTask() {
    // 1. Get Values
    let id = $('#taskId').val();
    let title = $('#taskTitle').val();
    let priority = $('#taskPriority').val();
    let deadline = $('#taskDeadline').val();
    let status = $('#taskStatus').val();
    let assignedTo = $('#taskAssignedTo').val();

    // 2. Validate
    if (!title || !deadline) {
        alert("Title and Deadline are required!");
        return;
    }

    // 3. Create Object
    let taskData = {
        TaskID: id,
        Title: title,
        Priority: priority,
        Deadline: deadline,
        Status: status,
        AssignedTo: parseInt(assignedTo) // Convert "5" to number 5
    };

    // 4. Send to Server
    $.ajax({
        url: '/Tasks/SaveTask',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(taskData),
        success: function (response) {
            location.reload();
        },
        error: function (xhr) {
            alert("Error: " + xhr.responseText);
        }
    });
}

function deleteTask(id) {
    if(confirm("Delete this task?")) {
        $.post('/Tasks/DeleteTask', { id: id }, function() {
            loadTasks();
        });
    }
}