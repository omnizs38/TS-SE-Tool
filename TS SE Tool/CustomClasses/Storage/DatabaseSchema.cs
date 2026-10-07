using System;
namespace TS_SE_Tool.Storage
{
    internal enum DatabaseKind { Profile, External }
    internal static class DatabaseSchema
    {
        internal const string Profile = @"CREATE TABLE DatabaseDetails (ID_DBline INTEGER PRIMARY KEY AUTOINCREMENT, GameName NVARCHAR(8) NOT NULL, SaveVersion INT NOT NULL, ProfileName NVARCHAR(128) NOT NULL, V1 numeric(4,0) NOT NULL, V2 numeric(4,0) NOT NULL, V3 numeric(4,0) NOT NULL, V4 numeric(4,0) NOT NULL, ReadableName NVARCHAR(30) NOT NULL);
CREATE TABLE Dependencies (ID_dep INTEGER PRIMARY KEY AUTOINCREMENT, Dependency NVARCHAR(256) NOT NULL);
CREATE TABLE CitysTable (ID_city INTEGER PRIMARY KEY AUTOINCREMENT, CityName NVARCHAR(32) NOT NULL);
CREATE UNIQUE INDEX [Idx_Uniq_CitysTable] ON [CitysTable] ([CityName]);
CREATE TABLE CompaniesTable (ID_company INTEGER PRIMARY KEY AUTOINCREMENT, CompanyName NVARCHAR(32) NOT NULL);
CREATE UNIQUE INDEX [Idx_Uniq_CompaniesTable] ON [CompaniesTable] ([CompanyName]);
CREATE TABLE CargoesTable (ID_cargo INTEGER PRIMARY KEY AUTOINCREMENT, CargoName NVARCHAR(32) NOT NULL);
CREATE UNIQUE INDEX [Idx_Uniq_CargoesTable] ON [CargoesTable] ([CargoName]);
CREATE TABLE TrailerDefinitionTable (ID_trailerD INTEGER PRIMARY KEY AUTOINCREMENT, TrailerDefinitionName NVARCHAR(64) NOT NULL);
CREATE UNIQUE INDEX [Idx_Uniq_TrailerDefinitionTable] ON [TrailerDefinitionTable] ([TrailerDefinitionName]);
CREATE TABLE CargoesToTrailerDefinitionTable (ID_trailerCtD INTEGER PRIMARY KEY AUTOINCREMENT, CargoID INT NOT NULL, TrailerDefinitionID INT NOT NULL, CargoType INT NOT NULL, FOREIGN KEY(CargoID) REFERENCES CargoesTable(ID_cargo), FOREIGN KEY(TrailerDefinitionID) REFERENCES TrailerDefinitionTable(ID_trailerD));
CREATE UNIQUE INDEX [Idx_Uniq_CargoesToTrailerDefinitionTable] ON [CargoesToTrailerDefinitionTable] ([CargoID],[TrailerDefinitionID],[CargoType]);
CREATE TABLE tempBulkCargoesToTrailerDefinitionTable (ID INTEGER PRIMARY KEY AUTOINCREMENT, CargoName NVARCHAR(32) NOT NULL, TrailerDefinitionName NVARCHAR(64) NOT NULL, CargoType INT NOT NULL);
CREATE TABLE tempCargoesToTrailerDefinitionTable (ID INTEGER PRIMARY KEY AUTOINCREMENT, CargoID INT NOT NULL, TrailerDefinitionID INT NOT NULL, CargoType INT NOT NULL);
CREATE TABLE TrailerVariantTable (ID_trailerV INTEGER PRIMARY KEY AUTOINCREMENT, TrailerVariantName NVARCHAR(64) NOT NULL);
CREATE UNIQUE INDEX [Idx_Uniq_TrailerVariantTable] ON [TrailerVariantTable] ([TrailerVariantName]);
CREATE TABLE TrailerDefinitionToTrailerVariantTable (ID_trailerDtV INTEGER PRIMARY KEY AUTOINCREMENT, TrailerDefinitionID INT NOT NULL, TrailerVariantID INT NOT NULL, FOREIGN KEY(TrailerDefinitionID) REFERENCES TrailerDefinitionTable(ID_trailerD), FOREIGN KEY(TrailerVariantID) REFERENCES TrailerVariantTable(ID_trailerV));
CREATE UNIQUE INDEX [Idx_Uniq_TrailerDefinitionToTrailerVariantTable] ON [TrailerDefinitionToTrailerVariantTable] ([TrailerDefinitionID],[TrailerVariantID]);
CREATE TABLE tempBulkTrailerDefinitionVariants (ID_trailerDtV INTEGER PRIMARY KEY AUTOINCREMENT, TrailerDefinitionName NVARCHAR(64) NOT NULL, TrailerVariantName NVARCHAR(32) NOT NULL);
CREATE TABLE tempTrailerDefinitionVariants (ID_trailerDtV INTEGER PRIMARY KEY AUTOINCREMENT, TrailerDefinitionID INT NOT NULL, TrailerVariantID INT NOT NULL);
CREATE TABLE TrucksTable (ID_truck INTEGER PRIMARY KEY AUTOINCREMENT, TruckName NVARCHAR(64) NOT NULL, TruckType TINYINT NOT NULL);
CREATE UNIQUE INDEX [Idx_TruckNameType] ON TrucksTable(TruckName,TruckType);
CREATE TABLE CompaniesInCitysTable (ID_CmpnToCt INTEGER PRIMARY KEY AUTOINCREMENT, CityID INT NOT NULL, CompanyID INT NOT NULL, FOREIGN KEY(CityID) REFERENCES CitysTable(ID_city) ON DELETE CASCADE, FOREIGN KEY(CompanyID) REFERENCES CompaniesTable(ID_company) ON DELETE CASCADE);
CREATE TABLE CompaniesCargoTable (ID_CmpnCrg INTEGER PRIMARY KEY AUTOINCREMENT, CompanyID INT NOT NULL, CargoID INT NOT NULL, FOREIGN KEY(CompanyID) REFERENCES CompaniesTable(ID_company) ON DELETE CASCADE, FOREIGN KEY(CargoID) REFERENCES CargoesTable(ID_cargo) ON DELETE CASCADE);
CREATE TABLE DistancesTable (ID_Distance INTEGER PRIMARY KEY AUTOINCREMENT, SourceCityID INT NOT NULL, SourceCompanyID INT NOT NULL, DestinationCityID INT NOT NULL, DestinationCompanyID INT NOT NULL, Distance INT NOT NULL, FerryTime INT NOT NULL, FerryPrice INT NOT NULL, FOREIGN KEY(SourceCityID) REFERENCES CitysTable(ID_city), FOREIGN KEY(SourceCompanyID) REFERENCES CompaniesTable(ID_company), FOREIGN KEY(DestinationCityID) REFERENCES CitysTable(ID_city), FOREIGN KEY(DestinationCompanyID) REFERENCES CompaniesTable(ID_company));
CREATE UNIQUE INDEX [Idx_Uniq_path_DistancesTable] ON [DistancesTable] ([SourceCityID],[SourceCompanyID],[DestinationCityID],[DestinationCompanyID]);
CREATE TABLE tempBulkDistancesTable (ID_Distance INTEGER PRIMARY KEY AUTOINCREMENT, SourceCity NVARCHAR(32) NOT NULL, SourceCompany NVARCHAR(32) NOT NULL, DestinationCity NVARCHAR(32) NOT NULL, DestinationCompany NVARCHAR(32) NOT NULL, Distance INT NOT NULL, FerryTime INT NOT NULL, FerryPrice INT NOT NULL);
CREATE TABLE tempDistancesTable (ID_Distance INTEGER PRIMARY KEY AUTOINCREMENT, SourceCityID INT NOT NULL, SourceCompanyID INT NOT NULL, DestinationCityID INT NOT NULL, DestinationCompanyID INT NOT NULL, Distance INT NOT NULL, FerryTime INT NOT NULL, FerryPrice INT NOT NULL);";
        internal const string External = @"CREATE TABLE BodyTypesTable (ID_bodytype INTEGER PRIMARY KEY AUTOINCREMENT, BodyTypeName NVARCHAR(32) NOT NULL);
CREATE TABLE CargoesTable (ID_cargo INTEGER PRIMARY KEY AUTOINCREMENT, CargoName NVARCHAR(32) NOT NULL, ADRclass INT NOT NULL, Fragility TEXT NOT NULL, Mass TEXT NOT NULL, UnitRewardpPerKM TEXT NOT NULL, Valuable BIT NOT NULL, Overweight BIT NOT NULL);
CREATE TABLE BodyTypesToCargoTable (ID_BodyToCrg INTEGER PRIMARY KEY AUTOINCREMENT, CargoID INT NOT NULL, BodyTypeID INT NOT NULL, FOREIGN KEY(CargoID) REFERENCES CargoesTable(ID_cargo), FOREIGN KEY(BodyTypeID) REFERENCES BodyTypesTable(ID_bodytype) ON DELETE CASCADE);
CREATE TABLE CompaniesTable (ID_company INTEGER PRIMARY KEY AUTOINCREMENT, CompanyName NVARCHAR(32) NOT NULL);
CREATE TABLE AllCargoesTable (ID_cargo INTEGER PRIMARY KEY AUTOINCREMENT, CargoName NVARCHAR(32) NOT NULL);
CREATE TABLE CompaniesCargoesInTable (ID_CargoIn INTEGER PRIMARY KEY AUTOINCREMENT, CompanyID INT NOT NULL, CargoID INT NOT NULL, FOREIGN KEY(CompanyID) REFERENCES CompaniesTable(ID_company) ON DELETE CASCADE, FOREIGN KEY(CargoID) REFERENCES AllCargoesTable(ID_cargo) ON DELETE CASCADE);
CREATE TABLE CompaniesCargoesOutTable (ID_CargoOut INTEGER PRIMARY KEY AUTOINCREMENT, CompanyID INT NOT NULL, CargoID INT NOT NULL, FOREIGN KEY(CompanyID) REFERENCES CompaniesTable(ID_company) ON DELETE CASCADE, FOREIGN KEY(CargoID) REFERENCES AllCargoesTable(ID_cargo) ON DELETE CASCADE);";
    }
}
