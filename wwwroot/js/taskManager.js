$(document).ready(function () {
    loadTasks();
    $('#searchInput').on('input', function() {
        loadTasks(); 
    });
});

function loadTasks() {
    let searchTerm = $('#searchInput').val() || '';

    $.get('/Tasks/GetTasks?term=' + searchTerm, function (data) {
        let html = '';
        if (data.length === 0) {
            html = '<tr><td colspan="6" class="text-center">No tasks found.</td></tr>';
        } else {
            data.forEach(t => {
                let badgeColor = t.priority === 'High' ? 'bg-danger' : 
                                 t.priority === 'Medium' ? 'bg-warning text-dark' : 'bg-success';

                // SAFETY FIX: Escape single quotes so descriptions like "User's Task" don't break the button
                let taskString = JSON.stringify(t).replace(/'/g, "&#39;");

                html += `
                    <tr>
                        <td><strong>${t.title}</strong></td>
                        <td>${t.assignedToName}</td>
                        <td><span class="badge ${badgeColor}">${t.priority}</span></td>
                        <td>${t.deadline}</td>
                        <td>${t.status}</td>
                        <td>
                            <button class="btn btn-sm btn-info" onclick='editTask(${taskString})'>Edit</button>
                            <button class="btn btn-sm btn-danger" onclick="deleteTask(${t.taskID})">Delete</button>
                        </td>
                    </tr>`;
            });
        }
        $('#taskTableBody').html(html);
    });
}

function openModal() {
    $('#taskId').val(0);
    $('#taskTitle').val('');
    $('#taskDesc').val(''); 
    $('#taskPriority').val('Medium');
    $('#taskDeadline').val('');
    $('#taskStatus').val('Pending');
    $('#taskAssignedTo').val(0);
    
    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function editTask(task) {
    $('#taskId').val(task.taskID);
    $('#taskTitle').val(task.title);
    
    // Fix: Handle lowercase 'description' or uppercase 'Description'
    let desc = task.description || task.Description || ''; 
    $('#taskDesc').val(desc);

    $('#taskPriority').val(task.priority);
    $('#taskDeadline').val(task.deadline);
    $('#taskStatus').val(task.status);
    $('#taskAssignedTo').val(task.assignedTo || 0);

    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function saveTask() {
    // 1. Get Values
    let id = $('#taskId').val();
    let title = $('#taskTitle').val();
    let desc = $('#taskDesc').val();
    let priority = $('#taskPriority').val();
    let deadline = $('#taskDeadline').val();
    let status = $('#taskStatus').val();
    let assignedToVal = $('#taskAssignedTo').val(); 

    // 2. Validate
    if (!title || !deadline) {
        alert("Title and Deadline are required!");
        return;
    }

    // 3. FIX: If value is "0" (Unassigned), send null. Else send the number.
    let finalAssignedTo = (assignedToVal === "0") ? null : parseInt(assignedToVal);

    let taskData = {
        TaskID: id,
        Title: title,
        Description: desc, 
        Priority: priority,
        Deadline: deadline,
        Status: status,
        AssignedTo: finalAssignedTo // <--- Uses the safe variable
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