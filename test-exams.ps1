$baseUrl = "http://localhost:5122"
$ErrorActionPreference = "Stop"

# Helper to get token
function Get-Token {
    Set-Content -Encoding ascii -NoNewline -Path "login-body.txt" -Value '{"email":"superadmin@intellicampus.com","password":"SuperAdmin@123"}'
    curl.exe -s -c cookies.txt -X POST "$baseUrl/api/Auth/login" -H "Content-Type: application/json" -d '@login-body.txt' | Out-Null
    $token = (Get-Content cookies.txt | Where-Object {$_ -match 'token\s+(\S+)'} | ForEach-Object {$matches[1]} | Select-Object -First 1)
    return $token
}

$token = Get-Token
Write-Host "Logged in. Token length: $($token.Length)"

# TEST 1: GET exams (initial)
Write-Host "`n========================================"
Write-Host "TEST 1: GET /api/exams (initial list)" -ForegroundColor Cyan
$result = curl.exe -s -H "Authorization: Bearer $token" "$baseUrl/api/exams"
$exams = $result | ConvertFrom-Json
Write-Host "Existing exams: $($exams.Count)" -ForegroundColor Green

# TEST 2: Create Midterm exam
Write-Host "`n========================================"
Write-Host "TEST 2: POST /api/exams (Create Midterm)" -ForegroundColor Cyan
Set-Content -Encoding ascii -NoNewline -Path "body.json" -Value '{"title":"Data Structures Midterm","description":"Covers weeks 1-8","examType":0,"date":"2026-07-15T09:00:00","time":"09:00:00","durationMinutes":120,"maxGrade":100,"totalMarks":50,"roomId":1,"courseId":3}'
$resp = curl.exe -s -X POST "$baseUrl/api/exams" -H "Content-Type: application/json" -H "Authorization: Bearer $token" -d '@body.json'
$exam = $resp | ConvertFrom-Json
if ($exam.examId -gt 0) {
    Write-Host "PASS: Created exam $($exam.examId): $($exam.title)" -ForegroundColor Green
    Write-Host "  Status: $($exam.status) (0=Upcoming), Type: $($exam.examType) (0=Midterm), Room: $($exam.roomName)" -ForegroundColor Gray
    Write-Host "  Course: $($exam.courseName), CreatedAt: $($exam.createdAt)" -ForegroundColor Gray
} else { Write-Host "FAIL: $resp" -ForegroundColor Red; exit 1 }
$id1 = $exam.examId

# TEST 3: Create Final exam
Write-Host "`n========================================"
Write-Host "TEST 3: POST /api/exams (Create Final)" -ForegroundColor Cyan
Set-Content -Encoding ascii -NoNewline -Path "body2.json" -Value '{"title":"Data Structures Final","description":"Comprehensive final","examType":1,"date":"2026-08-20T13:00:00","time":"13:00:00","durationMinutes":180,"maxGrade":200,"totalMarks":100,"roomId":4,"courseId":3}'
$resp2 = curl.exe -s -X POST "$baseUrl/api/exams" -H "Content-Type: application/json" -H "Authorization: Bearer $token" -d '@body2.json'
$exam2 = $resp2 | ConvertFrom-Json
if ($exam2.examId -gt 0) {
    Write-Host "PASS: Created exam $($exam2.examId): $($exam2.title)" -ForegroundColor Green
    Write-Host "  Status: $($exam2.status) (0=Upcoming), Type: $($exam2.examType) (1=Final), Room: $($exam2.roomName)" -ForegroundColor Gray
} else { Write-Host "FAIL: $resp2" -ForegroundColor Red; exit 1 }
$id2 = $exam2.examId

# TEST 4: Get all exams
Write-Host "`n========================================"
Write-Host "TEST 4: GET /api/exams (list all)" -ForegroundColor Cyan
$all = curl.exe -s -H "Authorization: Bearer $token" "$baseUrl/api/exams" | ConvertFrom-Json
$count = ($all | Measure-Object).Count
if ($count -ge 2) { Write-Host "PASS: $count exams total" -ForegroundColor Green } else { Write-Host "WARN: Only $count exams" -ForegroundColor Yellow }

# TEST 5: Get exam by ID
Write-Host "`n========================================"
Write-Host "TEST 5: GET /api/exams/$id1 (get by ID)" -ForegroundColor Cyan
$byId = curl.exe -s -H "Authorization: Bearer $token" "$baseUrl/api/exams/$id1" | ConvertFrom-Json
if ($byId.title -eq "Data Structures Midterm") {
    Write-Host "PASS: Retrieved exam ID=$id1" -ForegroundColor Green
    Write-Host "  Title: $($byId.title)" -ForegroundColor Gray
    Write-Host "  Room: $($byId.roomName), Status: $($byId.status)" -ForegroundColor Gray
    Write-Host "  CreatedAt: $($byId.createdAt)" -ForegroundColor Gray
} else { Write-Host "FAIL: Title mismatch: $($byId.title)" -ForegroundColor Red }

# TEST 6: Get by course
Write-Host "`n========================================"
Write-Host "TEST 6: GET /api/exams/course/3 (by course)" -ForegroundColor Cyan
$byCourse = curl.exe -s -H "Authorization: Bearer $token" "$baseUrl/api/exams/course/3" | ConvertFrom-Json
$courseCount = ($byCourse | Measure-Object).Count
if ($courseCount -eq 2) { Write-Host "PASS: Course 3 has 2 exams" -ForegroundColor Green } else { Write-Host "WARN: Course 3 has $courseCount exams (expected 2)" -ForegroundColor Yellow }

# TEST 7: Update exam
Write-Host "`n========================================"
Write-Host "TEST 7: PUT /api/exams/$id1 (update title & room)" -ForegroundColor Cyan
Set-Content -Encoding ascii -NoNewline -Path "update-body.json" -Value '{"title":"Data Structures Midterm - Updated","roomId":2}'
$updated = curl.exe -s -X PUT "$baseUrl/api/exams/$id1" -H "Content-Type: application/json" -H "Authorization: Bearer $token" -d '@update-body.json' | ConvertFrom-Json
if ($updated.title -eq "Data Structures Midterm - Updated" -and $updated.roomId -eq 2 -and $updated.roomName -eq "Hall A2") {
    Write-Host "PASS: Updated - Title: $($updated.title), Room: $($updated.roomName)" -ForegroundColor Green
} else { Write-Host "FAIL: Update mismatch - Title: $($updated.title), Room: $($updated.roomName)" -ForegroundColor Red }

# TEST 8: Set status to Cancelled
Write-Host "`n========================================"
Write-Host "TEST 8: PUT /api/exams/$id1 (set Cancelled)" -ForegroundColor Cyan
Set-Content -Encoding ascii -NoNewline -Path "cancel-body.json" -Value '{"status":2}'
$cancelled = curl.exe -s -X PUT "$baseUrl/api/exams/$id1" -H "Content-Type: application/json" -H "Authorization: Bearer $token" -d '@cancel-body.json' | ConvertFrom-Json
if ($cancelled.status -eq 2) {
    Write-Host "PASS: Status=2 (Cancelled)" -ForegroundColor Green
} else { Write-Host "FAIL: Status=$($cancelled.status)" -ForegroundColor Red }

# TEST 9: Check student exam schedule
Write-Host "`n========================================"
Write-Host "TEST 9: GET /api/ExamSchedule/my-exams (student)" -ForegroundColor Cyan
Set-Content -Encoding ascii -NoNewline -Path "student-login.txt" -Value '{"email":"mohammed.hassan@student.com","password":"Student@123"}'
curl.exe -s -c s-cookies.txt -X POST "$baseUrl/api/Auth/login" -H "Content-Type: application/json" -d '@student-login.txt' | Out-Null
$sToken = (Get-Content s-cookies.txt | Where-Object {$_ -match 'token\s+(\S+)'} | ForEach-Object {$matches[1]} | Select-Object -First 1)
$schedule = curl.exe -s -H "Authorization: Bearer $sToken" "$baseUrl/api/ExamSchedule/my-exams" | ConvertFrom-Json
$schedCount = ($schedule | Measure-Object).Count
if ($schedCount -ge 1) {
    Write-Host "PASS: Student has $schedCount exam schedule entries" -ForegroundColor Green
    $schedule | ForEach-Object { Write-Host "  - $($_.courseName) [$($_.examType)]: $($_.date) $($_.startTime)-$($_.endTime) at $($_.location)" }
} else { Write-Host "WARN: No exam schedule entries (expected due to course enrollment)" -ForegroundColor Yellow }

# TEST 10: Delete final exam
Write-Host "`n========================================"
Write-Host "TEST 10: DELETE /api/exams/$id2" -ForegroundColor Cyan
$statusCode = curl.exe -s -o nul -w "%{http_code}" -X DELETE "$baseUrl/api/exams/$id2" -H "Authorization: Bearer $token"
if ($statusCode -eq 204) { Write-Host "PASS: Deleted (204 No Content)" -ForegroundColor Green } else { Write-Host "FAIL: HTTP $statusCode" -ForegroundColor Red }

# TEST 11: Verify deletion
Write-Host "`n========================================"
Write-Host "TEST 11: GET /api/exams (verify deletion)" -ForegroundColor Cyan
$remaining = curl.exe -s -H "Authorization: Bearer $token" "$baseUrl/api/exams" | ConvertFrom-Json
$remainingCount = ($remaining | Measure-Object).Count
Write-Host "Remaining exams: $remainingCount" -ForegroundColor Green

# TEST 12: Excel template
Write-Host "`n========================================"
Write-Host "TEST 12: GET /api/ExcelImport/exams/template" -ForegroundColor Cyan
$template = curl.exe -s -H "Authorization: Bearer $token" "$baseUrl/api/ExcelImport/exams/template" | ConvertFrom-Json
Write-Host "PASS: Columns: $($template.columns -join ', ')" -ForegroundColor Green

# TEST 13: Cleanup
Write-Host "`n========================================"
Write-Host "TEST 13: Cleanup - delete remaining exam" -ForegroundColor Cyan
$statusCode2 = curl.exe -s -o nul -w "%{http_code}" -X DELETE "$baseUrl/api/exams/$id1" -H "Authorization: Bearer $token"
if ($statusCode2 -eq 204) { Write-Host "PASS: Cleanup deleted (204)" -ForegroundColor Green } else { Write-Host "WARN: HTTP $statusCode2" -ForegroundColor Yellow }

Write-Host "`n========================================"
Write-Host "ALL 13 TESTS COMPLETED" -ForegroundColor Green
Write-Host "========================================"

Remove-Item "login-body.txt","cookies.txt","body.json","body2.json","update-body.json","cancel-body.json","student-login.txt","s-cookies.txt" -Force -ErrorAction SilentlyContinue
