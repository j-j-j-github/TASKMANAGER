// GLOBAL VARIABLE: Current Sort Order
var currentSort = 'date-new'; // Default: Newest First

$(document).ready(function () {
    // 1. Setup the Dropdown
    initSelect2(); 

    // --- NEW: Smooth Dropdown Exit Logic ---
    $('.dropdown').on('hide.bs.dropdown', function (e) {
        const $menu = $(this).find('.dropdown-menu');
        
        // If we haven't added the 'hiding' class yet, prevent default close
        if (!$menu.hasClass('hiding')) {
            e.preventDefault(); 
            $menu.addClass('hiding'); // Play CSS animation
            
            // Wait 300ms (match CSS transition), then close for real
            setTimeout(function () {
                $menu.removeClass('hiding'); 
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

// --- SORTING LOGIC ---
function changeSort(sortType) {
    currentSort = sortType;
    loadTasks(); // Reload the list with the new sort order applied
}

// --- 2. LOAD TASKS (The Table + Sorting) ---
function loadTasks() {
    let term = $('#searchInput').val() || '';

    $.get('/Tasks/GetTasks', { term: term }, function (data) {
        let $tableBody = $('#taskTableBody');
        $tableBody.empty();

        if (!data || data.length === 0) {
             let emptyRow = '<tr><td colspan="6" class="text-center text-muted p-5">No tasks found. Create one to get started!</td></tr>';
             $tableBody.html(emptyRow);
             return;
        }

        // >>> SORTING LOGIC START <<<
        data.sort((a, b) => {
            // Helper to convert Priority to Number
            const pMap = { "High": 3, "Medium": 2, "Low": 1 };

            if (currentSort === 'date-new') {
                return new Date(b.deadline) - new Date(a.deadline);
            } else if (currentSort === 'date-old') {
                return new Date(a.deadline) - new Date(b.deadline);
            } else if (currentSort === 'prio-high') {
                return pMap[b.priority] - pMap[a.priority];
            } else if (currentSort === 'prio-low') {
                return pMap[a.priority] - pMap[b.priority];
            }
            return 0;
        });
        // >>> SORTING LOGIC END <<<

        // Generate Table Rows
        data.forEach(task => {
            // 1. Clean Data
            let taskString = JSON.stringify(task).replace(/'/g, "&#39;");

            // 2. Priority Badge
            let badgeClass = 
                task.priority === "High" ? "bg-danger bg-opacity-10 text-danger" :
                task.priority === "Medium" ? "bg-warning bg-opacity-10 text-warning" :
                "bg-success bg-opacity-10 text-success";

            // 3. Status Icon
            let statusIcon = 
                task.status === "Completed" ? '<i class="bi bi-check-circle-fill text-success"></i>' :
                task.status === "In Progress" ? '<i class="bi bi-arrow-repeat text-primary"></i>' :
                '<i class="bi bi-circle text-muted"></i>';

            // 4. Avatar Logic
            let userDisplay = `<span class="badge bg-light text-dark border">Unassigned</span>`;
            if (task.assignedToName && task.assignedToName !== "Unassigned") {
                let color = stringToColor(task.assignedToEmail || "default");
                let initial = getInitials(task.assignedToName);
                
                userDisplay = `
                <div class="d-flex align-items-center">
                    <div class="rounded-circle text-white d-flex align-items-center justify-content-center me-2 shadow-sm" 
                         style="width: 32px; height: 32px; background-color: ${color}; font-size: 0.8rem;">
                        ${initial}
                    </div>
                    <span class="small fw-bold text-dark">${task.assignedToName}</span>
                </div>`;
            }

            // 5. Action Buttons (Edit/Delete vs View Only)
            let actions = '';
            if (task.canManage) {
                actions = `
                    <button class="btn btn-sm btn-light text-primary hover-scale shadow-sm me-1" onclick='editTask(${taskString})'><i class="bi bi-pencil-fill"></i></button>
                    <button class="btn btn-sm btn-light text-danger hover-scale shadow-sm" onclick="deleteTask(${task.id})"><i class="bi bi-trash3-fill"></i></button>
                `;
            } else {
                 actions = `<button class="btn btn-sm btn-light text-secondary hover-scale shadow-sm" onclick='viewTask(${taskString})'><i class="bi bi-eye-fill"></i></button>`;
            }

            // 6. Build Row HTML
            let row = `
            <tr class="align-middle">
                <td class="ps-4">
                    <div class="fw-bold text-dark">${task.title}</div>
                    <div class="small text-muted text-truncate" style="max-width: 200px;">${task.description || ''}</div>
                </td>
                <td>${userDisplay}</td>
                <td><span class="badge ${badgeClass} px-3 py-2 rounded-pill">${task.priority}</span></td>
                <td class="small fw-bold text-secondary">${task.deadline}</td>
                <td><div class="d-flex align-items-center gap-2 small fw-bold">${statusIcon} <span>${task.status}</span></div></td>
                <td class="text-end pe-4">${actions}</td>
            </tr>`;
            
            $tableBody.append(row);
        });
    });
}

// --- 3. LOAD CHART (WORKLOAD DISTRIBUTION) ---
function loadChart() {
    $.get('/Tasks/GetTaskStats', function (data) {
        
        let labels = data.map(item => {
            if (item.name === "Unassigned") return "Unassigned";
            return `${item.name} (${item.email})`; 
        });
        
        let counts = data.map(item => item.count); 
        let colors = data.map(item => stringToColor(item.email || "default"));

        if (window.myPieChart) window.myPieChart.destroy();

        let ctx = document.getElementById('taskChart').getContext('2d');
        window.myPieChart = new Chart(ctx, {
            type: 'doughnut',
            data: { 
                labels: labels, 
                datasets: [{ 
                    data: counts, 
                    backgroundColor: colors,
                    hoverOffset: 10,
                    borderWidth: 0 
                }] 
            },
            options: { 
                responsive: true, 
                maintainAspectRatio: false, 
                plugins: { 
                    legend: { position: 'bottom', labels: { usePointStyle: true, padding: 20 } },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                let value = context.parsed || 0;
                                return ` ${value} Tasks`;
                            }
                        }
                    }
                },
                cutout: '65%', 
                layout: { padding: 20 }
            }
        });
    });
}

// --- 4. NOTIFICATIONS (Bell Icon) ---
function loadNotifications() {
    $.get('/Tasks/GetMyNotifications', function (tasks) {
        let count = tasks.length;
        let $badge = $('#notificationBadge');
        let $list = $('#notificationList');
        
        // Clear old list items except header
        $list.find('li:not(.dropdown-header)').remove(); 

        if (count > 0) {
            $badge.text(count).show();
            
            tasks.forEach(t => {
                let icon, colorClass, titleText;
                
                if (t.isOverdue) {
                    icon = '<div class="bg-danger bg-opacity-10 text-danger rounded-circle p-2 me-3"><i class="bi bi-exclamation-diamond-fill"></i></div>';
                    titleText = "Overdue";
                    colorClass = "text-danger";
                } else if (t.isDueToday) {
                    icon = '<div class="bg-warning bg-opacity-10 text-warning rounded-circle p-2 me-3"><i class="bi bi-alarm-fill"></i></div>';
                    titleText = "Due Today";
                    colorClass = "text-warning";
                } else {
                    icon = '<div class="bg-primary bg-opacity-10 text-primary rounded-circle p-2 me-3"><i class="bi bi-clock-history"></i></div>';
                    titleText = "Due Tomorrow";
                    colorClass = "text-primary";
                }
                
                let itemHtml = `
                <li>
                    <a class="dropdown-item d-flex align-items-center p-3 border-bottom" href="#">
                        ${icon}
                        <div>
                            <div class="small fw-bold ${colorClass}">${titleText}</div>
                            <div class="text-dark small text-truncate" style="max-width: 180px;">${t.title}</div>
                        </div>
                    </a>
                </li>`;
                
                $list.append(itemHtml);
            });
        } else {
            $badge.hide();
            $list.append('<li class="text-center p-4 text-muted small"><i class="bi bi-check-circle fs-4 d-block mb-2 text-success"></i>All caught up!</li>');
        }
    });
}

// --- 5. ADMIN ALERTS (Timeline Over) ---
function checkAdminOverdue() {
    $.get('/Tasks/GetAdminOverdueTasks')
        .done(function (tasks) {
            if (tasks && tasks.length > 0) {
                $('#adminAlertSection').fadeIn();
                let html = '';
                
                tasks.forEach(t => {
                    let color = stringToColor(t.assignedToEmail || "admin");
                    
                    html += `
                    <div class="bg-white border-0 shadow-sm rounded-3 p-3 d-flex align-items-center" style="min-width: 260px; border-left: 4px solid #dc3545 !important;">
                        <span style="width: 35px; height: 35px; background-color: ${color}; color: white; border-radius: 50%; text-align: center; line-height: 35px; font-weight: bold; font-size: 0.8rem; margin-right: 12px;">
                            ${getInitials(t.assignedToName)}
                        </span>
                        <div>
                            <div class="fw-bold text-dark" style="font-size: 0.9rem;">${t.assignedToName}</div>
                            <div class="text-muted text-truncate" style="font-size: 0.75rem; max-width: 150px;">${t.title}</div>
                            <div class="text-danger fw-bold" style="font-size: 0.7rem;">
                                <i class="bi bi-calendar-x me-1"></i>${t.deadline || t.dueDate.split('T')[0]}
                            </div>
                        </div>
                    </div>`;
                });
                $('#overdueList').html(html);
            }
        });
}

// --- MODAL FUNCTIONS (Create/Edit/View) ---

function openModal() {
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', false);
    $('#taskAssignedTo').prop('disabled', false);
    $('#taskModal .modal-footer .btn-primary').show();
    $('.modal-title').text('New Task'); 
    
    $('#taskId').val(0); 
    $('#taskTitle').val(''); 
    $('#taskDesc').val(''); 
    $('#taskPriority').val('Medium'); 
    $('#taskDeadline').val(''); 
    $('#taskStatus').val('Pending');
    $('#taskAssignedTo').val('0').trigger('change'); 
    
    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function editTask(task) {
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', false);
    $('#taskAssignedTo').prop('disabled', false);
    $('#taskModal .modal-footer .btn-primary').show();
    $('.modal-title').text('Edit Task');
    
    $('#taskId').val(task.id); 
    $('#taskTitle').val(task.title);
    $('#taskDesc').val(task.description || ''); 
    $('#taskPriority').val(task.priority);
    $('#taskDeadline').val(task.deadline); 
    $('#taskStatus').val(task.status);
    
    let assignedVal = task.assignedTo || '0';
    $('#taskAssignedTo').val(assignedVal.toString()).trigger('change');
    
    new bootstrap.Modal(document.getElementById('taskModal')).show();
}

function viewTask(task) {
    $('#taskId').val(task.id); 
    $('#taskTitle').val(task.title);
    $('#taskDesc').val(task.description || ''); 
    $('#taskPriority').val(task.priority);
    $('#taskDeadline').val(task.deadline); 
    $('#taskStatus').val(task.status);
    
    let assignedVal = task.assignedTo || '0';
    $('#taskAssignedTo').val(assignedVal.toString()).trigger('change');
    
    $('#taskModal input, #taskModal select, #taskModal textarea').prop('disabled', true);
    $('#taskAssignedTo').prop('disabled', true); 
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

    if (!title || !deadline) { alert("Title and Deadline are required!"); return; }
    
    let finalAssignedTo = (assignedToVal === "0") ? null : parseInt(assignedToVal);

    let taskData = { 
        Id: parseInt(id), 
        Title: title, 
        Description: desc, 
        Priority: priority, 
        DueDate: deadline,
        Status: status, 
        AssignedTo: finalAssignedTo 
    };

    $.ajax({
        url: '/Tasks/SaveTask',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(taskData),
        success: function () { 
            var modalEl = document.getElementById("taskModal");
            var modal = bootstrap.Modal.getInstance(modalEl);
            modal.hide();
            location.reload(); 
        },
        error: function (xhr) { 
            console.error(xhr.responseText);
            alert("Error saving task."); 
        }
    });
}

// --- NEW: DELETE TASK FUNCTIONS ---

function deleteTask(id) {
    if (!id) { alert("Error: Task ID not found."); return; }
    $('#deleteTaskIdHidden').val(id); // Set ID in hidden input
    new bootstrap.Modal(document.getElementById('deleteTaskModal')).show(); // Open the Mini Modal
}

function confirmDeleteTask() {
    let id = $('#deleteTaskIdHidden').val();
    let $btn = $('#deleteTaskModal .btn-danger');
    $btn.prop('disabled', true).text('Deleting...');

    $.post('/Tasks/DeleteTask', { id: id })
        .done(function() {
            let modalEl = document.getElementById('deleteTaskModal');
            let modal = bootstrap.Modal.getInstance(modalEl);
            modal.hide();
            loadTasks(); 
            loadChart();
            $btn.prop('disabled', false).text('Delete');
        })
        .fail(function(xhr) {
            alert("Error deleting task.");
            $btn.prop('disabled', false).text('Delete');
        });
}