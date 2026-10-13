-- Read-only: run on the SQL Server while Rep/save is waiting.
-- A DBA may need to run this with server-state monitoring permissions.
SELECT r.session_id, r.blocking_session_id, DB_NAME(r.database_id) AS database_name,
       r.status, r.command, r.wait_type, r.wait_time, r.wait_resource,
       r.total_elapsed_time, r.open_transaction_count,
       s.host_name, s.program_name,
       blocker.host_name AS blocking_host, blocker.program_name AS blocking_program
FROM sys.dm_exec_requests r
JOIN sys.dm_exec_sessions s ON s.session_id = r.session_id
LEFT JOIN sys.dm_exec_sessions blocker ON blocker.session_id = r.blocking_session_id
WHERE r.session_id <> @@SPID AND s.is_user_process = 1
ORDER BY r.blocking_session_id DESC, r.total_elapsed_time DESC;
-- Do not kill sessions or change isolation settings without investigating the owner.
