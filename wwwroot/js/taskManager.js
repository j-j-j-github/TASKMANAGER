// wwwroot/js/taskManager.js

$(document).ready(function () {
    loadTasks();
    loadChart(); // <--- This draws the pie chart on load

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
            
                let taskString = JSON.stringify(t).replace(/'/g, "&#39;");
                
                // Permission Logic: Edit/Delete vs View
                let actionButtons = '';
                if (t.canManage) {
                    actionButtons = `
                        <button class="btn btn-sm btn-info" onclick='editTask(${taskString})'>Edit</button>
                        <button class="btn btn-sm btn-danger" onclick="deleteTask(${t.taskID})">Delete</button>
                    `;
                } else {
                    actionButtons = `
                        <button class="btn btn-sm btn-secondary" onclick='viewTask(${taskString})'>View Details</button>
                    `;
                }
            
                html += `
                    <tr>
                        <td><strong>${t.title}</strong></td>
                        <td>${t.assignedToName}</td>
                        <td><span class="badge ${badgeColor}">${t.priority}</span></td>
                        <td>${t.deadline}</td>
                        <td>${t.status}</td>
                        <td>${actionButtons}</td>
                    </tr>`;
            });
        }
        $('#taskTableBody').html(html);
    });
}

// --- CHART LOGIC ---
function loadChart() {
    $.get('/Tasks/GetTaskStats', function (data) {
        let labels = data.map(item => item.name);
        let counts = data.map(item => item.count);
        let colors = ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF', '#FF9F40'];

        if (window.myPieChart) {
            window.myPieChart.destroy();
        }

        let ctx = document.getElementById('taskChart').getContext('2d');
        window.myPieChart = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    data: counts,
                    backgroundColor: colors
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false, // Prevents it from getting too big
                plugins: {
                    legend: { position: 'bottom' }
                }
            }
        });
    });
}

function openModal() {
    // Reset to "New Task" mode
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', false);
    $('#taskModal .modal-footer .btn-primary').show();
    $('.modal-title').text('New Task'); 

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
    // Reset to "Edit" mode
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', false);
    $('#taskModal .modal-footer .btn-primary').show();
    $('.modal-title').text('Edit Task');

    $('#taskId').val(task.taskID);
    $('#taskTitle').val(task.title);
    let desc = task.description || task.Description || ''; 
    $('#taskDesc').val(desc);
    $('#taskPriority').val(task.priority);
    $('#taskDeadline').val(task.deadline);
    $('#taskStatus').val(task.status);
    $('#taskAssignedTo').val(task.assignedTo || 0);

    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function viewTask(task) {
    // Fill Data
    $('#taskId').val(task.taskID);
    $('#taskTitle').val(task.title);
    let desc = task.description || task.Description || ''; 
    $('#taskDesc').val(desc);
    $('#taskPriority').val(task.priority);
    $('#taskDeadline').val(task.deadline);
    $('#taskStatus').val(task.status);
    $('#taskAssignedTo').val(task.assignedTo || 0);

    // Lock UI
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', true);
    $('#taskModal .modal-footer .btn-primary').hide();
    $('.modal-title').text('View Task Details');

    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function saveTask() {
    let id = $('#taskId').val();
    let title = $('#taskTitle').val();
    let desc = $('#taskDesc').val();
    let priority = $('#taskPriority').val();
    let deadline = $('#taskDeadline').val();
    let status = $('#taskStatus').val();
    let assignedToVal = $('#taskAssignedTo').val(); 

    if (!title || !deadline) {
        alert("Title and Deadline are required!");
        return;
    }

    let finalAssignedTo = (assignedToVal === "0") ? null : parseInt(assignedToVal);

    let taskData = {
        TaskID: id,
        Title: title,
        Description: desc, 
        Priority: priority,
        Deadline: deadline,
        Status: status,
        AssignedTo: finalAssignedTo
    };

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
            loadChart(); // Refresh chart after deleting!
        });
    }
}