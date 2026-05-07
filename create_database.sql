IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE TABLE [Labels] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Color] nvarchar(7) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Labels] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE TABLE [TaskStatuses] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Color] nvarchar(7) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TaskStatuses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [LoginId] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [Role] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE TABLE [LabelRequests] (
        [Id] int NOT NULL IDENTITY,
        [RequestedByUserId] int NOT NULL,
        [RequestedName] nvarchar(50) NOT NULL,
        [Reason] nvarchar(200) NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_LabelRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LabelRequests_Users_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE TABLE [TaskItems] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [StatusId] int NOT NULL,
        [Priority] int NOT NULL,
        [DueDate] datetime2 NULL,
        [ParentTaskId] int NULL,
        [IsRecurring] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RecurringTemplateId] int NULL,
        [CreatedByUserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TaskItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskItems_TaskItems_ParentTaskId] FOREIGN KEY ([ParentTaskId]) REFERENCES [TaskItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TaskItems_TaskStatuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [TaskStatuses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TaskItems_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE TABLE [TaskAssignees] (
        [TaskItemId] int NOT NULL,
        [UserId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TaskAssignees] PRIMARY KEY ([TaskItemId], [UserId]),
        CONSTRAINT [FK_TaskAssignees_TaskItems_TaskItemId] FOREIGN KEY ([TaskItemId]) REFERENCES [TaskItems] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskAssignees_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE TABLE [TaskLabels] (
        [TaskItemId] int NOT NULL,
        [LabelId] int NOT NULL,
        CONSTRAINT [PK_TaskLabels] PRIMARY KEY ([TaskItemId], [LabelId]),
        CONSTRAINT [FK_TaskLabels_Labels_LabelId] FOREIGN KEY ([LabelId]) REFERENCES [Labels] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TaskLabels_TaskItems_TaskItemId] FOREIGN KEY ([TaskItemId]) REFERENCES [TaskItems] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE TABLE [TaskShares] (
        [TaskItemId] int NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_TaskShares] PRIMARY KEY ([TaskItemId], [UserId]),
        CONSTRAINT [FK_TaskShares_TaskItems_TaskItemId] FOREIGN KEY ([TaskItemId]) REFERENCES [TaskItems] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskShares_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Color', N'CreatedAt', N'DisplayOrder', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[TaskStatuses]'))
        SET IDENTITY_INSERT [TaskStatuses] ON;
    EXEC(N'INSERT INTO [TaskStatuses] ([Id], [Color], [CreatedAt], [DisplayOrder], [IsActive], [Name], [UpdatedAt])
    VALUES (1, N''#9E9E9E'', ''2026-01-01T00:00:00.0000000Z'', 1, CAST(1 AS bit), N''未着手'', ''2026-01-01T00:00:00.0000000Z''),
    (2, N''#2196F3'', ''2026-01-01T00:00:00.0000000Z'', 2, CAST(1 AS bit), N''進行中'', ''2026-01-01T00:00:00.0000000Z''),
    (3, N''#FF9800'', ''2026-01-01T00:00:00.0000000Z'', 3, CAST(1 AS bit), N''レビュー中'', ''2026-01-01T00:00:00.0000000Z''),
    (4, N''#F44336'', ''2026-01-01T00:00:00.0000000Z'', 4, CAST(1 AS bit), N''保留'', ''2026-01-01T00:00:00.0000000Z''),
    (5, N''#4CAF50'', ''2026-01-01T00:00:00.0000000Z'', 5, CAST(1 AS bit), N''完了'', ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Color', N'CreatedAt', N'DisplayOrder', N'IsActive', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[TaskStatuses]'))
        SET IDENTITY_INSERT [TaskStatuses] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_LabelRequests_RequestedByUserId] ON [LabelRequests] ([RequestedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_LabelRequests_Status] ON [LabelRequests] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Labels_Name] ON [Labels] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_TaskAssignees_UserId] ON [TaskAssignees] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_TaskItems_CreatedByUserId] ON [TaskItems] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_TaskItems_DueDate] ON [TaskItems] ([DueDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_TaskItems_ParentTaskId] ON [TaskItems] ([ParentTaskId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_TaskItems_StatusId] ON [TaskItems] ([StatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_TaskLabels_LabelId] ON [TaskLabels] ([LabelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE INDEX [IX_TaskShares_UserId] ON [TaskShares] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaskStatuses_Name] ON [TaskStatuses] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_LoginId] ON [Users] ([LoginId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260504041842_AddLabelRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260504041842_AddLabelRequests', N'8.0.26');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Type] int NOT NULL,
        [TaskItemId] int NULL,
        [Message] nvarchar(500) NOT NULL,
        [IsRead] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_TaskItems_TaskItemId] FOREIGN KEY ([TaskItemId]) REFERENCES [TaskItems] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE TABLE [RecurringTemplates] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [Priority] int NOT NULL,
        [Frequency] int NOT NULL,
        [WeekDays] nvarchar(50) NULL,
        [DayOfMonth] int NULL,
        [ExcludeWeekends] bit NOT NULL,
        [ExcludeHolidays] bit NOT NULL,
        [GenerationTime] time NOT NULL,
        [DefaultStatusId] int NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedByUserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RecurringTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecurringTemplates_TaskStatuses_DefaultStatusId] FOREIGN KEY ([DefaultStatusId]) REFERENCES [TaskStatuses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RecurringTemplates_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE TABLE [RecurringTemplateAssignees] (
        [RecurringTemplateId] int NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_RecurringTemplateAssignees] PRIMARY KEY ([RecurringTemplateId], [UserId]),
        CONSTRAINT [FK_RecurringTemplateAssignees_RecurringTemplates_RecurringTemplateId] FOREIGN KEY ([RecurringTemplateId]) REFERENCES [RecurringTemplates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RecurringTemplateAssignees_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE TABLE [RecurringTemplateLabels] (
        [RecurringTemplateId] int NOT NULL,
        [LabelId] int NOT NULL,
        CONSTRAINT [PK_RecurringTemplateLabels] PRIMARY KEY ([RecurringTemplateId], [LabelId]),
        CONSTRAINT [FK_RecurringTemplateLabels_Labels_LabelId] FOREIGN KEY ([LabelId]) REFERENCES [Labels] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RecurringTemplateLabels_RecurringTemplates_RecurringTemplateId] FOREIGN KEY ([RecurringTemplateId]) REFERENCES [RecurringTemplates] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE TABLE [RecurringTemplateShares] (
        [RecurringTemplateId] int NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_RecurringTemplateShares] PRIMARY KEY ([RecurringTemplateId], [UserId]),
        CONSTRAINT [FK_RecurringTemplateShares_RecurringTemplates_RecurringTemplateId] FOREIGN KEY ([RecurringTemplateId]) REFERENCES [RecurringTemplates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RecurringTemplateShares_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_Notifications_TaskItemId] ON [Notifications] ([TaskItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_RecurringTemplates_IsActive] ON [RecurringTemplates] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_RecurringTemplates_GenerationTime] ON [RecurringTemplates] ([GenerationTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_RecurringTemplates_DefaultStatusId] ON [RecurringTemplates] ([DefaultStatusId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_RecurringTemplates_CreatedByUserId] ON [RecurringTemplates] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_RecurringTemplateAssignees_UserId] ON [RecurringTemplateAssignees] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_RecurringTemplateLabels_LabelId] ON [RecurringTemplateLabels] ([LabelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    CREATE INDEX [IX_RecurringTemplateShares_UserId] ON [RecurringTemplateShares] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505000001_AddNotificationsAndRecurringTemplates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505000001_AddNotificationsAndRecurringTemplates', N'8.0.26');
END;
GO

COMMIT;
GO

-- =============================================
-- 初期ユーザー（試験用）
-- admin / admin123  → Role=0 (Admin)
-- member / member123 → Role=1 (Member)
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [LoginId] = N'admin')
BEGIN
    INSERT INTO [Users] ([LoginId], [DisplayName], [PasswordHash], [Role], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES (
        N'admin',
        N'管理者',
        N'$2a$11$GcFFOeBbycoo5HWox24jbOrtP6I6UhZfnl1SoOLXm0atj4c4yRa0q',
        0,
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [LoginId] = N'member')
BEGIN
    INSERT INTO [Users] ([LoginId], [DisplayName], [PasswordHash], [Role], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES (
        N'member',
        N'メンバー',
        N'$2a$11$Q53cucxi/gQvQgsDGi9yOOke4a024UHvddsmCE9gg5QzfQde0XTXy',
        1,
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );
END;
GO

-- =============================================================================
-- 20260506: AddTeamsAndAssigneeTeamId
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Teams')
BEGIN
    CREATE TABLE [Teams] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [CreatedByUserId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Teams] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Teams_Users_CreatedByUserId]
            FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Teams_CreatedByUserId] ON [Teams] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TeamMembers')
BEGIN
    CREATE TABLE [TeamMembers] (
        [TeamId] int NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_TeamMembers] PRIMARY KEY ([TeamId], [UserId]),
        CONSTRAINT [FK_TeamMembers_Teams_TeamId]
            FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TeamMembers_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_TeamMembers_UserId] ON [TeamMembers] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'TaskItems' AND COLUMN_NAME = 'AssigneeTeamId'
)
BEGIN
    ALTER TABLE [TaskItems] ADD [AssigneeTeamId] int NULL;
    ALTER TABLE [TaskItems]
        ADD CONSTRAINT [FK_TaskItems_Teams_AssigneeTeamId]
            FOREIGN KEY ([AssigneeTeamId]) REFERENCES [Teams] ([Id]) ON DELETE SET NULL;
    CREATE INDEX [IX_TaskItems_AssigneeTeamId] ON [TaskItems] ([AssigneeTeamId]);
END;
GO
