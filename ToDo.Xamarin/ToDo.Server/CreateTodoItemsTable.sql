-- =========================================
-- Create table template SQL Azure Database 
-- Script Date: 6/21/2014 10:49:58 PM
-- =========================================

IF OBJECT_ID('dbo.TodoItems', 'U') IS NOT NULL
  DROP TABLE [dbo].[TodoItems]
GO

CREATE TABLE [dbo].[TodoItems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Description] [nvarchar](30) NOT NULL,
	[Notes] [nvarchar](max) NULL,
	[CreatedAt] [datetime] NOT NULL,
	[IsDone] [bit] NOT NULL,
	[IsArchived] [bit] NOT NULL,
 CONSTRAINT [PK_dbo.TodoItems] PRIMARY KEY CLUSTERED ([Id] ASC)
 )
GO
