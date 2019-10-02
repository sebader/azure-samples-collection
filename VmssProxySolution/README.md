# Scalable and high available squid proxy solution using VMSS

This solution builds a proxy solution using the open source Squid program to access certain white-listed DNS names from resources within an Azure VNET.

This is a rather light-weight solution compared to using something like Azure Firewall.

The ARM template deploys a zone-redundant Virtual Machine Scale Set (VMSS) with Ubuntu 18.04, uses cloud-init to install Squid and write the required configuration. The white-listed DNS names are sources from a simple text file that can easily be updated once the cluster is up and running.

## How to deploy

1) The first thing you need to do is to adjust the `sites.whitelist.txt` file with your desired DNS names. Then, upload the file for instance to an Azure Blob Storage account and create a read-only SAS token for it. You should then have a URL like this: `https://sample.blob.core.windows.net/mycontainer/sites.whitelist.txt?st=2019-09-10T14%3A04%3A33Z&se=2019-09-11T14%3A04%3A33Z&sp=rl&sv=2018-03-28&sr=b&sig=*********`

2) Open the `azuredeploy.json` and look for the field `"customData": "[base64('#cloud-config...`
Inside that field, look for the sample URL to the file and exchange it with your URL

3) Now go to https://ms.portal.azure.com/#create/Microsoft.Template and use your modified template. Fill out the parameters as required. Note: Since the template uses Availability Zones, make sure to pick an Azure Region which has Zone-support. If that is not an option, modify the template accordingly (Remove the part `"zones": ["1","3"]` from the template)

4) You're good to go! You have now deployed a VMSS inside a VNET, with a internal load balancer in front. You can reach your squid proxy on the private IP of the Load Balancer (something like `10.0.0.4)`) on port `3128`.


## How to update DNS whitelist

To update the whitelist file (add new DNS names or remove existing ones) in a running VMSS, you can use the following commands, for example from the Azure Cloud Shell:

Save the following content as `config.json`. Adjust the `fileUris` to point to your new whitelist file.

````
{
  "fileUris": ["https://{sample}.blob.core.windows.net/mycontainer/sites.whitelist.txt?st=2019-09-10T14%3A04%3A33Z&se=2019-09-11T14%3A04%3A33Z&sp=rl&sv=2018-03-28&sr=b&sig=*********"],
  "commandToExecute": "sudo cp sites.whitelist.txt /etc/squid/sites.whitelist.txt && sudo squid -k reconfigure"
}
````

Now, execute the following command to run the custom script extension on each node of the VMSS. This will download the new white list file and replace the existing one. Afterwards it calls Squid to reconfigure to pull the new config.

````
az vmss extension set --publisher Microsoft.Azure.Extensions --version 2.0 --name CustomScript --vmss-name {ScaleSetName} --settings @config.json -g {ResourceGroupName}
````

## Notes

A few things to note about the presented template:

- The template contains an Azure Bastion host for easy debugging. You can use that as a jump host onto the nodes of the VMSS. If you don't need or want that, just remove it from the template. It also deploys the required subnet `AzureBastionSubnet` into the VNET.

- Of course, you might want to deploy the VMSS and the internal LB into an existing VNET. This is absolutely possible. You just need to modify the corresponding sections of the ARM template.

- The template also deployed a public Load Balancer. This is necessary, as otherwise the nodes of the VMSS, which have only a private IP, don't have any NAT-way to get out to the internet. The public load balancer thus contains a dummy-rule on port 65000. As long as nothing is running on that port on the nodes, this is not a problem.

- This repo contains a sample cloud-init configuration file, which is much easier to read and modify then the string inside the `customData` field of the ARM template. To bring this config into a shape that can be used in the template, you have to remove all line breaks and escape chars. You can use this handy script to do that: https://github.com/anhowe/azure-util/blob/master/deployer/templates/vmsscloudinit/cloudinit/gen-oneline.py