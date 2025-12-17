$(document).ready(function () {
    // 1. Setup the Dropdown
    initSelect2(); 
    // --- NEW: Smooth Dropdown Exit Logic ---
    $('.dropdown').on('hide.bs.dropdown', function (e) {
        const $menu = $(this).find('.dropdown-menu');
        
        // If we haven't added the 'hiding' class yet, it means this is the first attempt to close
        if (!$menu.hasClass('hiding')) {
            e.preventDefault(); // 1. STOP Bootstrap from closing immediately
            $menu.addClass('hiding'); // 2. Play the 'slideOut' CSS animation
            
            // 3. Wait 300ms (match the CSS time), then close for real
            setTimeout(function () {
                $menu.removeClass('hiding'); // Remove our temporary class
                
                // 4. Tell Bootstrap to close it officially now
                // Note: This triggers the event again, but the 'if' check above prevents a loop
                const toggle = e.currentTarget.querySelector('[data-bs-toggle="dropdown"]');
                bootstrap.Dropdown.getOrCreateInstance(toggle).hide();
            }, 300); 
        }
    });
    
    // 2. Load Data
    loadTasks();
    loadChart(); 
    
    // 3. Check Notifications
    loadNotifications();
    checkAdminOverdue();

    // 4. Search Listener
    $('#searchInput').on('input', function() {
        loadTasks(); 
    });
});

// --- HELPER: Generate Color from Email ---
function stringToColor(str) {
    if (!str || str === "Unassigned") return "#cccccc"; 
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
        hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    let c = (hash & 0x00FFFFFF).toString(16).toUpperCase();
    return "#" + "00000".substring(0, 6 - c.length) + c;
}

// --- HELPER: Get Initials ---
function getInitials(name) {
    if (!name || name === "Unassigned") return "?";
    return name.match(/(\b\S)?/g).join("").match(/(^\S|\S$)?/g).join("").toUpperCase();
}

// --- 1. RICH DROPDOWN (Select2) ---
function initSelect2() {
    let data = [{ id: '0', text: '-- Unassigned --', email: '' }];
    
    if (window.availableUsers) {
        window.availableUsers.forEach(u => {
            data.push({
                id: u.id,
                text: `${u.name} (${u.email})`, 
                email: u.email 
            });
        });
    }

    $('#taskAssignedTo').select2({
        data: data,
        width: '100%', 
        dropdownParent: $('#taskModal'), 
        templateResult: formatUserOption, 
        templateSelection: formatUserOption 
    });
}

function formatUserOption(user) {
    if (!user.id || user.id === '0') return user.text; 
    let color = stringToColor(user.email);
    return $(`<span><span style="color:${color}; font-size: 1.2em; margin-right: 5px;">●</span>${user.text}</span>`);
}

// --- 2. LOAD TASKS (The Table) ---
function loadTasks() {
    let searchTerm = $('#searchInput').val() || '';

    $.get('/Tasks/GetTasks?term=' + searchTerm, function (data) {
        let html = '';
        if (!data || data.length === 0) {
            html = '<tr><td colspan="6" class="text-center">No tasks found.</td></tr>';
        } else {
            data.forEach(t => {
                let badgeColor = t.priority === 'High' ? 'bg-danger' : 
                                 t.priority === 'Medium' ? 'bg-warning text-dark' : 'bg-success';
                let taskString = JSON.stringify(t).replace(/'/g, "&#39;");
                
                // Avatar Logic
                let userDisplay = "Unassigned";
                if(t.assignedToName !== "Unassigned") {
                    let color = stringToColor(t.assignedToEmail);
                    let initials = getInitials(t.assignedToName);
                    let avatar = `<span style="display:inline-block; width:30px; height:30px; background-color:${color}; color:white; text-align:center; line-height:30px; margin-right:8px; font-weight:bold; border-radius: 4px;">${initials}</span>`;
                    
                    userDisplay = `<div class="d-flex align-items-center">${avatar}<div><div>${t.assignedToName}</div><small class="text-muted" style="font-size:0.8em">(${t.assignedToEmail})</small></div></div>`;
                }

                let actionButtons = t.canManage 
                    ? `<button class="btn btn-sm btn-info" onclick='editTask(${taskString})'>Edit</button> <button class="btn btn-sm btn-danger" onclick="deleteTask(${t.taskID})">Delete</button>`
                    : `<button class="btn btn-sm btn-secondary" onclick='viewTask(${taskString})'>View Details</button>`;
            
                html += `<tr><td class="align-middle"><strong>${t.title}</strong></td><td class="align-middle">${userDisplay}</td><td class="align-middle"><span class="badge ${badgeColor}">${t.priority}</span></td><td class="align-middle">${t.deadline}</td><td class="align-middle">${t.status}</td><td class="align-middle">${actionButtons}</td></tr>`;
            });
        }
        $('#taskTableBody').html(html);
    });
}

// --- 3. LOAD CHART (Fixed Labels) ---
function loadChart() {
    $.get('/Tasks/GetTaskStats', function (data) {
        let labels = data.map(item => {
             if(item.name === "Unassigned") return "Unassigned";
             return `${item.name} (${item.email})`; 
        });
        let counts = data.map(item => item.count);
        let colors = data.map(item => stringToColor(item.email));

        if (window.myPieChart) window.myPieChart.destroy();

        let ctx = document.getElementById('taskChart').getContext('2d');
        window.myPieChart = new Chart(ctx, {
            type: 'pie',
            data: { labels: labels, datasets: [{ data: counts, backgroundColor: colors }] },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
        });
    });
}

// --- 4. NOTIFICATIONS (Bell) ---
function loadNotifications() {
    $.get('/Tasks/GetMyNotifications', function (tasks) {
        let count = tasks.length;
        let $badge = $('#notificationBadge');
        let $list = $('#notificationList');
        
        $list.find('li:not(.dropdown-header, .dropdown-divider)').remove(); 

        if (count > 0) {
            $badge.text(count).show();
            tasks.forEach(t => {
                let icon = t.isOverdue ? '<i class="fa-solid fa-triangle-exclamation"></i>' : 
                           t.isDueToday ? '<i class="fa-solid fa-calendar-day"></i>' : '<i class="fa-solid fa-hourglass-half"></i>';
                let colorClass = t.isOverdue ? "text-danger fw-bold" : t.isDueToday ? "text-warning fw-bold" : "text-primary";
                let message = t.isOverdue ? `Overdue: ${t.title}` : t.isDueToday ? `Due Today: ${t.title}` : `Due Tomorrow: ${t.title}`;
                
                $list.append(`<li><a class="dropdown-item ${colorClass}" href="#">${icon} ${message}</a></li>`);
            });
        } else {
            $badge.hide();
            $list.append('<li class="text-center p-2 text-muted small">No pending deadlines!</li>');
        }
    });
}

// --- 5. ADMIN ALERTS ---
function checkAdminOverdue() {
    $.get('/Tasks/GetAdminOverdueTasks')
        .done(function (tasks) {
            if (tasks && tasks.length > 0) {
                $('#adminAlertSection').show();
                let html = '';
                tasks.forEach(t => {
                    let color = stringToColor(t.assignedToEmail);
                    html += `<div class="bg-white border rounded p-2 d-flex align-items-center shadow-sm" style="min-width: 200px;">
                        <span style="display:inline-block; width:10px; height:30px; background-color:${color}; margin-right:10px; border-radius: 2px;"></span>
                        <div><div class="fw-bold text-danger">${t.assignedToName}</div><small class="text-muted d-block" style="font-size:0.85em">${t.title}</small><small class="text-danger fw-bold" style="font-size:0.75em">Overdue: ${t.deadline}</small></div>
                    </div>`;
                });
                $('#overdueList').html(html);
            }
        })
        .fail(() => console.log("Not admin or error checking overdue."));
}

// --- MODAL FUNCTIONS ---
function openModal() {
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', false);
    $('#taskAssignedTo').prop('disabled', false);
    $('#taskModal .modal-footer .btn-primary').show();
    $('.modal-title').text('New Task'); 
    $('#taskId').val(0); $('#taskTitle').val(''); $('#taskDesc').val(''); 
    $('#taskPriority').val('Medium'); $('#taskDeadline').val(''); $('#taskStatus').val('Pending');
    $('#taskAssignedTo').val('0').trigger('change'); 
    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function editTask(task) {
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', false);
    $('#taskAssignedTo').prop('disabled', false);
    $('#taskModal .modal-footer .btn-primary').show();
    $('.modal-title').text('Edit Task');
    $('#taskId').val(task.taskID); $('#taskTitle').val(task.title);
    $('#taskDesc').val(task.description || ''); $('#taskPriority').val(task.priority);
    $('#taskDeadline').val(task.deadline); $('#taskStatus').val(task.status);
    let assignedVal = task.assignedTo || '0';
    $('#taskAssignedTo').val(assignedVal.toString()).trigger('change');
    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function viewTask(task) {
    $('#taskId').val(task.taskID); $('#taskTitle').val(task.title);
    $('#taskDesc').val(task.description || ''); $('#taskPriority').val(task.priority);
    $('#taskDeadline').val(task.deadline); $('#taskStatus').val(task.status);
    let assignedVal = task.assignedTo || '0';
    $('#taskAssignedTo').val(assignedVal.toString()).trigger('change');
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', true);
    $('#taskAssignedTo').prop('disabled', true); 
    $('#taskModal .modal-footer .btn-primary').hide();
    $('.modal-title').text('View Task Details');
    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function saveTask() {
    let id = $('#taskId').val(); let title = $('#taskTitle').val();
    let desc = $('#taskDesc').val(); let priority = $('#taskPriority').val();
    let deadline = $('#taskDeadline').val(); let status = $('#taskStatus').val();
    let assignedToVal = $('#taskAssignedTo').val();

    if (!title || !deadline) { alert("Title and Deadline are required!"); return; }
    let finalAssignedTo = (assignedToVal === "0") ? null : parseInt(assignedToVal);

    let taskData = { TaskID: id, Title: title, Description: desc, Priority: priority, Deadline: deadline, Status: status, AssignedTo: finalAssignedTo };

    $.ajax({
        url: '/Tasks/SaveTask',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(taskData),
        success: function () { location.reload(); },
        error: function (xhr) { alert("Error: " + xhr.responseText); }
    });
}

function deleteTask(id) {
    if(confirm("Delete this task?")) {
        $.post('/Tasks/DeleteTask', { id: id }, function() { loadTasks(); loadChart(); });
    }
}