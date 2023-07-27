# AzSqlMigration

This solution would help to migrate SQL database from source to destination quick and fast. 

Steps are as follows
Step 1: Export Database into blob container in source resource group
Step 2: Copy the blob container to target from source
Step 3: Import database bacpac file from target blob container
Step 4: Verify table records
