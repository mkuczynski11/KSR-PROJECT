<template>
  <b-container>
    <b-tabs>
      <b-tab title="Books">
        <b-row class="mt-4">
          <b-col cols="8"></b-col>
          <b-col cols="4">
            <b-button @click="openBookCreationForm()" variant="warning">
              Add book
            </b-button>
          </b-col>
        </b-row>
        <b-row v-if="booksLoaded" class="py-4">
          <b-col cols="1"></b-col>
          <b-col>
            <b-table
              cols="10"
              v-if="booksLoaded"
              :fields="fields"
              :items="bookItems"
              show-empty
            >
              <template #cell(id)="data">
                {{ data.item.id }}
              </template>
              <template #cell(name)="data">
                {{ data.item.name }}
              </template>
              <template #cell(details)="row">
                <b-button
                  size="sm"
                  @click="row.toggleDetails"
                  class="mr-2"
                  :variant="row.detailsShowing ? 'warning' : 'success'"
                >
                  {{ row.detailsShowing ? "Hide" : "Show" }}
                </b-button>
              </template>
              <template #empty="">
                <h4><b>No books found</b></h4>
              </template>
              <template #row-details="row">
                <b-card>
                  <b-row class="pt-2">
                    <b-col cols="3">
                      <b>Price:</b>
                    </b-col>
                    <b-col cols="3">
                      <b-form-input
                        v-model="row.item.unitPrice"
                        :state="
                          row.item.unitPrice ==
                          bookBaseItems.find((book) => book.id == row.item.id)
                            .unitPrice
                        "
                        :type="'number'"
                      />
                    </b-col>
                    <b-col cols="3">
                      <b>Discount:</b>
                    </b-col>
                    <b-col cols="3">
                      <b-form-input
                        v-model="row.item.discount"
                        :type="'number'"
                        :state="
                          row.item.discount ==
                          bookBaseItems.find((book) => book.id == row.item.id)
                            .discount
                        "
                      />
                    </b-col>
                  </b-row>
                  <b-row class="pt-2">
                    <b-col cols="3">
                      <b>Quantity:</b>
                    </b-col>
                    <b-col cols="3">
                      <b-form-input
                        v-model="row.item.quantity"
                        :type="'number'"
                        :state="
                          row.item.quantity ==
                          bookBaseItems.find((book) => book.id == row.item.id)
                            .quantity
                        "
                    /></b-col>
                    <b-col cols="3">
                      <b>Name:</b>
                    </b-col>
                    <b-col cols="3">
                      <b-form-input
                        v-model="row.item.name"
                        :type="'text'"
                        :state="
                          row.item.name ==
                          bookBaseItems.find((book) => book.id == row.item.id)
                            .name
                        "
                      />
                    </b-col>
                  </b-row>
                  <b-row>
                    <b-col cols="9"></b-col>
                    <b-col cols="3">
                      <b-button
                        @click="saveBookChanges(row.item)"
                        class="mt-4"
                        size="md"
                        variant="info"
                        >Save changes</b-button
                      >
                    </b-col>
                  </b-row>
                </b-card>
              </template>
            </b-table>
          </b-col>
          <b-col cols="1"></b-col>
        </b-row>
        <b-row v-else>
          <b-col cols="12" class="py-4">
            <b-spinner style="width: 3rem; height: 3rem" />
          </b-col>
        </b-row>
        <b-modal id="book-creation-modal" size="lg" :hide-header="true">
          <b-container>
            <h4>Create new book form</h4>
            <hr />
            <b-row class="pt-2">
              <b-col cols="3">
                <b>Name</b>
              </b-col>
              <b-col cols="3">
                <b-form-input v-model="bookCreationForm.name" type="text" />
              </b-col>
              <b-col cols="3">
                <b>Quantity</b>
              </b-col>
              <b-col cols="3">
                <b-form-input
                  v-model="bookCreationForm.quantity"
                  type="number"
                />
              </b-col>
            </b-row>
            <b-row class="pt-4">
              <b-col cols="3">
                <b>Price</b>
              </b-col>
              <b-col cols="3">
                <b-form-input v-model="bookCreationForm.price" type="number" />
              </b-col>
              <b-col cols="3">
                <b>Discount</b>
              </b-col>
              <b-col cols="3">
                <b-form-input
                  v-model="bookCreationForm.discount"
                  type="number"
                />
              </b-col>
            </b-row>
          </b-container>
          <template #modal-footer="{}">
            <b-button variant="danger" @click="closeBookCreationForm()">
              Cancel
            </b-button>
            <b-button
              variant="success"
              @click="createBook()"
              :disabled="!validBookForm()"
              >Create book</b-button
            >
          </template>
        </b-modal>
      </b-tab>
      <b-tab title="Shipping">
        <b-row class="mt-4">
          <b-col cols="8"></b-col>
          <b-col cols="4">
            <b-button @click="openShippingCreationForm()" variant="warning">
              Add shipping
            </b-button>
          </b-col>
        </b-row>
        <b-row v-if="shippingMethodsLoaded" class="py-4">
          <b-col cols="1"></b-col>
          <b-col>
            <b-table
              cols="10"
              v-if="shippingMethodsLoaded"
              :fields="shippingFields"
              :items="shippingItems"
              show-empty
            >
              <template #cell(name)="data">
                {{ data.item.name }}
              </template>
              <template #cell(price)="data">
                {{ data.item.price }}
              </template>
              <template #cell(delete)="row">
                <b-button
                  size="sm"
                  @click="deleteShipping(row.item)"
                  class="mr-2"
                  variant="danger"
                >
                  Delete
                </b-button>
              </template>
            </b-table>
          </b-col>
          <b-col cols="1"></b-col>
        </b-row>
        <b-modal id="shipping-creation-modal" size="lg" :hide-header="true">
          <b-container>
            <h4>Create new shipping form</h4>
            <hr />
            <b-row class="pt-2">
              <b-col cols="3">
                <b>Name</b>
              </b-col>
              <b-col cols="3">
                <b-form-input v-model="shippingCreationForm.name" type="text" />
              </b-col>
              <b-col cols="3">
                <b>Price</b>
              </b-col>
              <b-col cols="3">
                <b-form-input
                  v-model="shippingCreationForm.price"
                  type="number"
                />
              </b-col>
            </b-row>
          </b-container>
          <template #modal-footer="{}">
            <b-button variant="danger" @click="closeShippingCreationForm()">
              Cancel
            </b-button>
            <b-button
              variant="success"
              @click="createShipping()"
              :disabled="!validShippingForm()"
              >Add shipping</b-button
            >
          </template>
        </b-modal>
      </b-tab>
    </b-tabs>
  </b-container>
</template>

<script>
import {
  getBooks,
  getShippingMethods,
  getBooksPrices,
  getBooksDiscounts,
  getShippingPrices,
  createBook,
  updateBookInWarehouse,
  updateBookPrice,
  updateBookDiscount,
  createShipping,
  deleteShippingMethod,
} from "@/api/api.js";

const BOOK_FIELDS = ["ID", "name", "details"];
const SHIPPING_FIELDS = ["Name", "price", "delete"];

export default {
  name: "ManagementPanel",
  data: function () {
    return {
      books: [],
      shippingMethods: [],
      bookItems: [],
      bookBaseItems: [],
      shippingItems: [],
      fields: BOOK_FIELDS,
      shippingFields: SHIPPING_FIELDS,
      booksLoaded: false,
      shippingMethodsLoaded: false,
      form: {
        quantity: null,
        price: null,
        discount: null,
        name: null,
      },
      bookCreationForm: {
        name: null,
        quantity: null,
        discount: null,
        price: null,
      },
      shippingCreationForm: {
        name: null,
        price: null,
      },
      bookToReserve: null,
    };
  },

  async mounted() {
    await this.parseBooks();
    await this.parseShippingMethods();
  },

  methods: {
    async loadBooks() {
      let res = await getBooks();
      this.books = res;
    },

    async loadPrices() {
      let res = await getBooksPrices();

      return res;
    },

    async loadShippingPrices() {
      let res = await getShippingPrices();

      return res;
    },

    async loadDiscounts() {
      let res = await getBooksDiscounts();

      return res;
    },

    async parseBooks() {
      this.booksLoaded = false;
      await this.loadBooks();
      this.bookItems = [];

      let prices = await this.loadPrices();
      let discounts = await this.loadDiscounts();

      for (let book of this.books) {
        let newBook = JSON.parse(JSON.stringify(book));

        for (let p of prices) {
          if (p.id == book.id) {
            newBook.unitPrice = p.price;
            break;
          }
        }
        for (let d of discounts) {
          if (d.id == book.id) {
            newBook.discount = d.discount;
            break;
          }
        }
        this.bookItems.push(newBook);
      }

      this.bookBaseItems = JSON.parse(JSON.stringify(this.bookItems));

      this.booksLoaded = true;
    },

    async parseShippingMethods() {
      this.shippingMethodsLoaded = true;
      this.shippingMethods = await getShippingMethods();
      this.shippingItems = [];

      for (let method of this.shippingMethods) {
        let newMethod = JSON.parse(JSON.stringify(method));
        this.shippingItems.push({
          name: newMethod.methodValue,
          price: newMethod.price,
        });
      }

      this.shippingMethodsLoaded = true;
    },

    validBookForm() {
      if (
        this.bookCreationForm["name"] === "" ||
        this.bookCreationForm["quantity"] === "" ||
        this.bookCreationForm["discount"] === "" ||
        this.bookCreationForm["price"] === "" ||
        parseInt(this.bookCreationForm["quantity"]) <= 0 ||
        parseInt(this.bookCreationForm["discount"]) < 0 ||
        parseInt(this.bookCreationForm["discount"]) > 100 ||
        parseInt(this.bookCreationForm["price"]) < 0
      ) {
        return false;
      }
      return true;
    },

    validShippingForm() {
      if (
        this.bookCreationForm["name"] == "" ||
        this.bookCreationForm["price"] < 0
      ) {
        return false;
      }
      return true;
    },

    async saveBookChanges(book) {
      let idToUpdate = this.bookBaseItems.indexOf(
        this.bookBaseItems.find((item) => item.id == book.id)
      );

      let updatePrice =
        book.price != this.bookBaseItems.find((b) => b.id == book.id).price;
      let updateQuantity =
        book.quantity !=
        this.bookBaseItems.find((b) => b.id == book.id).quantity;
      let updateDiscount =
        book.discount !=
        this.bookBaseItems.find((b) => b.id == book.id).discount;
      let updateName =
        book.name != this.bookBaseItems.find((b) => b.id == book.id).name;

      let res = null;

      if (updatePrice) {
        res = await updateBookPrice(book);
      }
      if (updateQuantity || updateName) {
        res = await updateBookInWarehouse(book);
      }
      if (updateDiscount) {
        res = await updateBookDiscount(book);
      }

      if (res.status === 200) {
        this.$bvToast.toast(`Changes saved correctly!`, {
          title: "Book changes",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to save changes!`, {
          title: "Book changes",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      this.bookBaseItems[idToUpdate] = book;
      this.bookItems = JSON.parse(JSON.stringify(this.bookItems));
    },

    openBookCreationForm() {
      this.bookCreationForm.name = "DefaultName";
      this.bookCreationForm.price = 49;
      this.bookCreationForm.quantity = 1;
      this.bookCreationForm.discount = 0;

      this.$bvModal.show("book-creation-modal");
    },

    closeBookCreationForm() {
      this.$bvModal.hide("book-creation-modal");
    },

    async createBook() {
      let res = await createBook(this.bookCreationForm);

      if (res.status === 200) {
        this.$bvToast.toast(`Book created correctly!`, {
          title: "Book changes",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to create book!`, {
          title: "Book changes",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      this.parseBooks();
      this.closeBookCreationForm();
    },

    openShippingCreationForm() {
      this.shippingCreationForm.name = "ShippingName";
      this.shippingCreationForm.price = 14;

      this.$bvModal.show("shipping-creation-modal");
    },

    closeShippingCreationForm() {
      this.$bvModal.hide("shipping-creation-modal");
    },

    async createShipping() {
      let res = await createShipping(this.shippingCreationForm);

      if (res.status === 200) {
        this.$bvToast.toast(`Created shipping method correctly!`, {
          title: "Shipping changes",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to create shipping method!`, {
          title: "Shipping changes",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      await this.parseShippingMethods();
      this.closeShippingCreationForm();
      this.shippingItems = JSON.parse(JSON.stringify(this.shippingItems));
    },

    async deleteShipping(method) {
      let res = await deleteShippingMethod(method);

      if (res.status === 200) {
        this.$bvToast.toast(`Deleted shipping method correctly!`, {
          title: "Shipping changes",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to delete shipping method!`, {
          title: "Shipping changes",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      await this.parseShippingMethods();
      this.shippingItems = this.shippingItems.filter(
        (item) => item.name != method.name
      );

      this.shippingItems = JSON.parse(JSON.stringify(this.shippingItems));
    },
  },
};
</script>

<style lang="scss" scoped></style>
