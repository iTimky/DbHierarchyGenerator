namespace DbHierarchyGenerator.Templates
{
    public static class SqlTemplates
    {
        public const string CreateNonFkConstraintTemplate = @"            alter table [{0}].[{1}] add constraint {2} {3} ({4});";
        public const string CreateIndexTemplate = @"            create{0} {1} index {2} on [{3}].[{4}]({5})";
        public const string CreateFkConstraintTemplate = @"            alter table [{0}].[{1}] add constraint {2} foreign key ({3}) references [{4}].[{5}]({6});";
        public const string DropFkConstraintTemplate = @"            alter table [{0}].[{1}] drop constraint {2};";
        public const string DropFunctionTemplate = @"            if exists (select 1 from sys.objects where object_id = object_id('{0}.{1}') and type in ('FN', 'TF'))
                drop function {0}.{1};";
        public const string DropProcedureTemplate = @"            if exists (select 1 from sys.objects where object_id = object_id('{0}.{1}') and type in ('P', 'PC'))
                drop procedure {0}.{1};";
        public const string DropTableTemplate = @"            drop table {0}.{1};";
        public const string DropCreateTableTypeTemplate = @"            -- drop all dependent functions/procedures
{2}
            
            -- drop type
            if exists (select 1 from sys.types where schema_name(schema_id) = '{0}' and name = '{1}' and is_table_type = 1)
                drop type {0}.{1};

            create type {0}.{1} as table
            (
                
            );";
        public const string PartitionCreateTemplate = @"            create partition function [{0}]({1}) as range left for values ();
            create partition scheme [{2}] as partition [{0}] to ({3});            
            alter partition scheme [{2}] next used [{4}];
            alter partition function [{0}]() split range ({5});";
        public const string AddHistoryPartitionsJobStepTemplate = @"            declare @DbName sysname = db_name();
            declare @JobName sysname = N'{0}';
            declare @StepName sysname = N'{1}_AddRangeIfNeeded';
            exec msdb.dbo.sp_add_jobstep
                    @job_name = @JobName,
                    @step_name = @StepName,
		            @command = N'{2};', 
		            @database_name = @DbName,
                    @on_success_action = 1; -- Quit with success

            declare @StepId int = (select step_id from msdb.dbo.sysjobsteps where step_name = @StepName);
            if @StepId <> 1
            begin
                declare @PreviousStepId int = @StepId - 1;
                exec msdb.dbo.sp_update_jobstep @job_name = @JobName, @step_id = @PreviousStepId, @on_success_action = 3 -- Go to next step
            end;";
        public const string IntPartitionFunctionExecTemplate = @"exec dbo.AddIntRangedPartitionsIfNeeded ''{0}'', {1}";
        public const string DatePartitionFunctionExecTemplate = @"exec dbo.AddDateRangedPartitionsIfNeeded ''{0}'', ''{1}'', {2}";
        public const string CreateFunctionTemplate = @"            if not exists (select 1 from sys.objects where object_id = object_id('{0}.{1}') and type in ('FN', 'TF'))
                exec ('create function [{0}].[{1}] returns int as begin return 1; end;');";
        public const string CreateProcedureTemplate = @"            if not exists (select 1 from sys.objects where object_id = object_id('{0}.{1}') and type in ('P', 'PC'))
                exec ('create procedure [{0}].[{1}] as;');";
        public const string CreateView = @"            if not exists (select 1 from sys.objects where object_id = object_id('{0}.{1}') and type = 'V')
                exec ('create view [{0}].[{1}] as select 1 as Col;');";
        public const string CreateTrigger = @"            if not exists (select 1 from sys.objects where object_id = object_id('{0}.{1}') and type = 'TR')
                exec ('create trigger [{0}].[{1}] on [{2}].[{3}] after insert, update, delete as declare @a int;');";

        public const string CreateTableTemplate = @"            create table [{0}].[{1}]
            (
{2}
            );{3}";
        public const string AlterHistoryTriggerTemplate = @"            alter trigger [{0}].[{1}] on [{2}].[{3}]
            after insert, update, delete not for replication as
            begin

                    if @@rowcount = 0
                        return;

                    set nocount on;

                    declare @UserId int, @UserIp nvarchar(32);
                    select @UserId = UserId, @UserIp = UserIp
                    from dbo.SessionInfo
                    where SPID = @@spid;

                    if @UserId is null
                        throw 50000, 'There is no user in SessionInfo', 1;

{4}

            end;";
        public const string AlterHistoryTriggerInnerTemplate = @"               if (select count(1) from inserted) <> 0 and (select count(1) from deleted) <> 0
                begin
                        select  {7}
                        into #Inserted_History
                        from inserted;

                        select  {7}
                        into #Deleted_History
                        from deleted;

                        declare @Ids table ({0});
                        insert @Ids
                        exec dbo.GetDiffIds '{1}';

                        insert {2} ({3}, [Action_History], [UserId_History], [UserIp_History])
                        select  {5},
                                'U',
                                @UserId,
                                @UserIp
                        from @Ids ids
                        join inserted i on {4};
                    end
                    else if (select count(1) from inserted) <> 0
                    begin
                        insert {2} ({3}, [Action_History], [UserId_History], [UserIp_History])
                        select  {5},
                                'I',
                                @UserId,
                                @UserIp
                        from inserted i;
                    end
                    else
                    begin
                        insert {2} ({3}, [Action_History], [UserId_History], [UserIp_History])
                        select  {6},
                                'D',
                                @UserId,
                                @UserIp
                        from deleted d;
                    end";
    }
}