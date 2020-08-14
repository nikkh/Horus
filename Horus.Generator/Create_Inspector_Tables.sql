IF  NOT EXISTS (SELECT * FROM sys.objects 
WHERE object_id = OBJECT_ID(N'[dbo].[GeneratedDocuments]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[GeneratedDocuments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Account] [nvarchar](50) NULL,
	[SingleName] [nvarchar](50) NULL,
	[AddressLine1] [nvarchar](50) NULL,
	[AddressLine2] [nvarchar](50) NULL,
	[PostalCode] [nvarchar](50) NULL,
	[City] [nvarchar](50) NULL,
	[Notes] [nvarchar](50) NULL,
	[DocumentNumber] [nvarchar](50) NOT NULL,
	[FileName] [nvarchar](50) NULL,
	[DocumentFormat] [nvarchar](50) NULL,
	[DocumentDate] [datetime2](7) NULL,
	[PreTaxTotalValue] [decimal](19, 5) NULL,
	[TaxTotalValue] [decimal](19, 5) NULL,
	[ShippingTotalValue] [decimal](19, 5) NULL,
	[GrandTotalValue]  [decimal](19, 5) NULL,
	[LineNumber] [nvarchar](5) NOT NULL,
	[Title] [nvarchar](50) NULL,
	[Author] [nvarchar](50) NULL,
	[Isbn] [nvarchar](50) NULL,
	[Quantity] [decimal](19, 5) NULL,
	[Discount] [decimal](19, 5) NULL,
	[Price] [decimal](19, 5) NULL,
	[Taxable] [bit] NOT NULL,
	[GoodsValue] [decimal](19, 5) NULL,
	[DiscountValue] [decimal](19, 5) NULL,
	[DiscountedGoodsValue] [decimal](19, 5) NULL,
	[TaxableValue] [decimal](19, 5) NULL

PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

IF  NOT EXISTS (SELECT * FROM sys.objects 
WHERE object_id = OBJECT_ID(N'[dbo].[ScoreSummary]') AND type in (N'U'))
BEGIN

CREATE TABLE [dbo].[ScoreSummary](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Team] [nvarchar](50) NOT NULL,
	[TotalScore] [int] NOT NULL,
	[InspectionTime] [datetime2](7) NOT NULL

PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

IF  NOT EXISTS (SELECT * FROM sys.objects 
WHERE object_id = OBJECT_ID(N'[dbo].[ScoreDetail]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[ScoreDetail](
	[Id] [int] IDENTITY(1,1) NOT NULL, [Team] [nvarchar](50) NOT NULL, [InspectionTime] [datetime2](7) NOT NULL, [Type] [nvarchar](50) NOT NULL, [Notes] [nvarchar] (max) NULL, [Score] [int] NOT NULL, [Status] [nvarchar](15)  NOT NULL,
	
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END