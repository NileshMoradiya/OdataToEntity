USE [OdataToEntity]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

if exists (select * from sysobjects where id = object_id('dbo.GetOrders') and objectproperty(id, 'IsProcedure') = 1)
	drop procedure dbo.GetOrders;
go

if exists (select * from sysobjects where id = object_id('dbo.ResetDb') and objectproperty(id, 'IsProcedure') = 1)
	drop procedure dbo.ResetDb;
go

if exists (select * from sysobjects where id = object_id('dbo.ResetManyColumns') and objectproperty(id, 'IsProcedure') = 1)
	drop procedure dbo.ResetManyColumns;
go

if exists (select * from sysobjects where id = object_id('dbo.ScalarFunction') and objectproperty(id, 'IsScalarFunction') = 1)
	drop function dbo.ScalarFunction;
go

if exists (select * from sysobjects where id = object_id('dbo.ScalarFunctionWithParameters') and objectproperty(id, 'IsScalarFunction') = 1)
	drop function dbo.ScalarFunctionWithParameters;
go

if exists (select * from sysobjects where id = object_id('dbo.TableFunction') and objectproperty(id, 'IsInlineFunction') = 1)
	drop function dbo.TableFunction;
go

if exists (select * from sysobjects where id = object_id('dbo.TableFunctionWithCollectionParameter') and objectproperty(id, 'IsInlineFunction') = 1)
	drop function dbo.TableFunctionWithCollectionParameter;
go

if exists (select * from sysobjects where id = object_id('dbo.TableFunctionWithParameters') and objectproperty(id, 'IsInlineFunction') = 1)
	drop function dbo.TableFunctionWithParameters;
go

if exists (select * from sysobjects where id = object_id('dbo.ManyColumnsView') and objectproperty(id, 'IsView') = 1)
	drop view dbo.ManyColumnsView;
go

if exists (select * from sysobjects where id = object_id('dbo.OrderItemsView') and objectproperty(id, 'IsView') = 1)
	drop view dbo.OrderItemsView;
go

if exists (select * from sysobjects where id = object_id('dbo.OrderItems') and objectproperty(id, 'IsTable') = 1)
	drop table dbo.OrderItems;
go

if exists (select * from sysobjects where id = object_id('dbo.CustomerShippingAddress') and objectproperty(id, 'IsTable') = 1)
	drop table dbo.CustomerShippingAddress;
go

if exists (select * from sysobjects where id = object_id('dbo.ShippingAddresses') and objectproperty(id, 'IsTable') = 1)
	drop table dbo.ShippingAddresses;
go

if exists (select * from sysobjects where id = object_id('dbo.Orders') and objectproperty(id, 'IsTable') = 1)
	drop table dbo.Orders;
go

if exists (select * from sysobjects where id = object_id('dbo.Categories') and objectproperty(id, 'IsTable') = 1)
	drop table dbo.Categories;
go

if exists (select * from sysobjects where id = object_id('dbo.Customers') and objectproperty(id, 'IsTable') = 1)
	drop table dbo.Customers;
go

if exists (select * from sysobjects where id = object_id('dbo.ManyColumns') and objectproperty(id, 'IsTable') = 1)
	drop table dbo.ManyColumns;
go

if type_id('string_list') is not null
	drop type dbo.string_list;

create type dbo.string_list as table (item varchar(max))
go

CREATE TABLE [dbo].[Categories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](128) NOT NULL,
	[ParentId] [int] NULL,
	[DateTime] [datetime2]
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Customers](
	[Address] [varchar](256) NULL,
	[Country] [char](2) NOT NULL,
	[Id] [int] NOT NULL,
	[Name] [varchar](128) NOT NULL,
	[Sex] [int] NULL,
 CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED 
(
	[Country],[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ManyColumns](
	[Column01] [int] NOT NULL,
	[Column02] [int] NOT NULL,
	[Column03] [int] NOT NULL,
	[Column04] [int] NOT NULL,
	[Column05] [int] NOT NULL,
	[Column06] [int] NOT NULL,
	[Column07] [int] NOT NULL,
	[Column08] [int] NOT NULL,
	[Column09] [int] NOT NULL,
	[Column10] [int] NOT NULL,
	[Column11] [int] NOT NULL,
	[Column12] [int] NOT NULL,
	[Column13] [int] NOT NULL,
	[Column14] [int] NOT NULL,
	[Column15] [int] NOT NULL,
	[Column16] [int] NOT NULL,
	[Column17] [int] NOT NULL,
	[Column18] [int] NOT NULL,
	[Column19] [int] NOT NULL,
	[Column20] [int] NOT NULL,
	[Column21] [int] NOT NULL,
	[Column22] [int] NOT NULL,
	[Column23] [int] NOT NULL,
	[Column24] [int] NOT NULL,
	[Column25] [int] NOT NULL,
	[Column26] [int] NOT NULL,
	[Column27] [int] NOT NULL,
	[Column28] [int] NOT NULL,
	[Column29] [int] NOT NULL,
	[Column30] [int] NOT NULL,
	CONSTRAINT [PK_ManyColumns] PRIMARY KEY CLUSTERED
(
	[Column01] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[OrderItems](
	[Count] [int] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[Price] [decimal](18, 2) NULL,
	[Product] [varchar](256) NOT NULL,
 CONSTRAINT [PK_OrderItem] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Orders](
	[AltCustomerCountry] [char](2) NULL,
	[AltCustomerId] [int] NULL,
	[CustomerCountry] [char](2) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[Date] [datetimeoffset](7) NULL,
	[Dummy] [int] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](256) NOT NULL,
	[Status] [int] NOT NULL,
 CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ShippingAddresses](
	[OrderId] [int] NOT NULL,
	[Id] [int] NOT NULL,
	[Address] [varchar](256) NOT NULL,
 CONSTRAINT [PK_ShippingAddresses] PRIMARY KEY CLUSTERED 
(
	[OrderId],[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[CustomerShippingAddress](
	[CustomerCountry] [char](2) NOT NULL,
	[CustomerId] [int] NOT NULL,
	[ShippingAddressOrderId] [int] NOT NULL,
	[ShippingAddressId] [int] NOT NULL,
 CONSTRAINT [PK_CustomerShippingAddress] PRIMARY KEY CLUSTERED 
(
	[CustomerCountry] ASC,
	[CustomerId] ASC,
	[ShippingAddressOrderId] ASC,
	[ShippingAddressId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Categories]  WITH CHECK ADD  CONSTRAINT [FK_Categories_Categories] FOREIGN KEY([ParentId])
REFERENCES [dbo].[Categories] ([Id])
GO
ALTER TABLE [dbo].[Categories] CHECK CONSTRAINT [FK_Categories_Categories]
GO

ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD  CONSTRAINT [FK_OrderItem_Order] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([Id])
GO
ALTER TABLE [dbo].[OrderItems] CHECK CONSTRAINT [FK_OrderItem_Order]
GO

ALTER TABLE [dbo].[ShippingAddresses]  WITH CHECK ADD  CONSTRAINT [FK_ShippingAddresses_Order] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([Id])
GO
ALTER TABLE [dbo].[ShippingAddresses] CHECK CONSTRAINT [FK_ShippingAddresses_Order]
GO

ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [FK_Orders_AltCustomers] FOREIGN KEY([AltCustomerCountry],[AltCustomerId])
REFERENCES [dbo].[Customers] ([Country],[Id])
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [FK_Orders_AltCustomers]
GO

ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [FK_Orders_Customers] FOREIGN KEY([CustomerCountry],[CustomerId])
REFERENCES [dbo].[Customers] ([Country],[Id])
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [FK_Orders_Customers]
GO

ALTER TABLE [dbo].[CustomerShippingAddress]  WITH CHECK ADD  CONSTRAINT [FK_CustomerShippingAddress_Customers] FOREIGN KEY([CustomerCountry], [CustomerId])
REFERENCES [dbo].[Customers] ([Country], [Id])
GO
ALTER TABLE [dbo].[CustomerShippingAddress] CHECK CONSTRAINT [FK_CustomerShippingAddress_Customers]
GO
ALTER TABLE [dbo].[CustomerShippingAddress]  WITH CHECK ADD  CONSTRAINT [FK_CustomerShippingAddress_ShippingAddresses] FOREIGN KEY([ShippingAddressOrderId], [ShippingAddressId])
REFERENCES [dbo].[ShippingAddresses] ([OrderId], [Id])
GO
ALTER TABLE [dbo].[CustomerShippingAddress] CHECK CONSTRAINT [FK_CustomerShippingAddress_ShippingAddresses]
GO

CREATE procedure [dbo].[GetOrders]
  @id int,
  @name varchar(256),
  @status int
as
begin
	set nocount on;

	if @id is null and @name is null and @status is null
	begin
	  select * from dbo.Orders;
	  return;
	end;

	if not @id is null
	begin
	  select * from dbo.Orders where Id = @id;
	  return;
	end;

	if not @name is null
	begin
	  select * from dbo.Orders where Name like '%' + @name + '%';
	  return;
	end;

	if not @status is null
	begin
	  select * from dbo.Orders where Status = @status;
	  return;
	end;
end
go

CREATE procedure [dbo].[ResetDb]
as
begin
	set nocount on;

	delete from dbo.CustomerShippingAddress;
	delete from dbo.ShippingAddresses;
	delete from dbo.OrderItems;
	delete from dbo.Orders;
	delete from dbo.Customers;
	delete from dbo.Categories;

	dbcc checkident('dbo.OrderItems', reseed, 0);
	dbcc checkident('dbo.Orders', reseed, 0);
	dbcc checkident('dbo.Categories', reseed, 0);
end
go

CREATE procedure [dbo].[ResetManyColumns]
as
begin
	set nocount on;

	delete from dbo.ManyColumns;
end
go

create function [dbo].[ScalarFunction]()
returns int as
begin
	declare @count int;
	select @count = count(*) from dbo.Orders;
	return @count;
end
go

create function [dbo].[ScalarFunctionWithParameters](@id int, @name varchar(256), @status int)
returns int as
begin
	declare @count int;
	select @count = count(*) from dbo.Orders where Id = @id or Name like '%' + @name + '%' or Status = @status;
	return @count;
end
go

create function [dbo].[TableFunction]() returns table 
as return 
(
	select * from dbo.Orders
)
go

create function [dbo].[TableFunctionWithCollectionParameter](@items dbo.string_list readonly) returns table 
as return 
(
	select * from @items
)
go

create function [dbo].[TableFunctionWithParameters](@id int, @name varchar(256), @status int) returns table 
as return 
(
	select * from dbo.Orders where Id = @id or Name like '%' + @name + '%' or Status = @status
)
go

create view [dbo].[ManyColumnsView] with schemabinding as
	with n as
	(
		select 1 as num
		union all
		select num + 1 from n where num + 1 <= 100
	),
	num as
	(
		select row_number() over(order by (select null)) num from n n1, n n2, n n3
	)
	select 
		cast(num + 00 as int) Column01,
		cast(num + 01 as int) Column02,
		cast(num + 02 as int) Column03,
		cast(num + 03 as int) Column04,
		cast(num + 04 as int) Column05,
		cast(num + 05 as int) Column06,
		cast(num + 06 as int) Column07,
		cast(num + 07 as int) Column08,
		cast(num + 08 as int) Column09,
		cast(num + 09 as int) Column10,
		cast(num + 10 as int) Column11,
		cast(num + 11 as int) Column12,
		cast(num + 12 as int) Column13,
		cast(num + 13 as int) Column14,
		cast(num + 14 as int) Column15,
		cast(num + 15 as int) Column16,
		cast(num + 16 as int) Column17,
		cast(num + 17 as int) Column18,
		cast(num + 18 as int) Column19,
		cast(num + 19 as int) Column20,
		cast(num + 20 as int) Column21,
		cast(num + 21 as int) Column22,
		cast(num + 22 as int) Column23,
		cast(num + 23 as int) Column24,
		cast(num + 24 as int) Column25,
		cast(num + 25 as int) Column26,
		cast(num + 26 as int) Column27,
		cast(num + 27 as int) Column28,
		cast(num + 28 as int) Column29,
		cast(num + 29 as int) Column30
	from num;
go

create view [dbo].[OrderItemsView] with schemabinding as
	select o.Name, i.Product from dbo.Orders o inner join dbo.OrderItems i on o.Id = i.OrderId;
go
